use std::env;

use rayon::iter::{IntoParallelRefIterator, ParallelIterator};
use serde::Deserialize;

use crate::{fuel::Fuel, vec2::Vec2};

mod vec2;
mod fuel;
mod ball;
mod wall;

fn main() {
    let internal_config = awake();
    let config = &internal_config.config;

    let permutations: Vec<(f64, f64)> = (0..config.xRes)
        .flat_map(|i| {
            let x = config.minX + (i as f64) * (config.maxX - config.minX) / (config.xRes as f64);
            (0..config.vxRes).map(move |j| {
                let vx = config.minVX + (j as f64) * (config.maxVX - config.minVX) / (config.vxRes as f64);
                (x, vx)
            })
        })
        .collect();

    let results: Vec<Vec<Option<Trajectory>>> = permutations
        .par_iter()
        .map(|(x, vx)| {
            (0..config.angleRes)
                .map(|i| {
                    let angle = config.minAngle + (i as f64) * (config.maxAngle - config.minAngle) / (config.angleRes as f64);
                    binary_search(*x, *vx, angle, internal_config.clone())
                })
                .collect()
        })
        .collect();

    let best_trajectories: Vec<Trajectory> = results
        .iter()
        .filter_map(|trajectories| evaluate_trajectories(trajectories, internal_config.clone()))
        .collect();


}

fn awake() -> InternalConfig {
    let mut data_input_path = "shooter.json".to_string();

    if get_arg("--inputpath") != "" {
        data_input_path = get_arg("--inputpath");
    }

    let mut hood_output_path = "hoodPolynomial.json".to_string();
    let mut flywheel_output_path = "flywheelPolynomial.json".to_string();
    let mut tof_output_path = "tofPolynomial.json".to_string();

    if get_arg("--outputdir") != "" {
        let outdir = get_arg("--outputdir");
        let sep = if outdir.ends_with('/') { "" } else { "/" };
        hood_output_path = format!("{}{}{}", outdir, sep, hood_output_path);
        flywheel_output_path = format!("{}{}{}", outdir, sep, flywheel_output_path);
        tof_output_path = format!("{}{}{}", outdir, sep, tof_output_path); // was incorrectly using flywheel_output_path
    }

    let json = std::fs::read_to_string(&data_input_path).unwrap();
    let config: ShooterConfig = serde_json::from_str(&json).unwrap();

    let d_comp: f64 = config.rHood - config.rRol - config.rFly;
    let r_comp: f64 = d_comp / 2.0;
    let launch_point_r = r_comp + config.rFly; // derived from r_comp, no duplication

    InternalConfig {
        config,
        data_input_path,
        hood_output_path,
        flywheel_output_path,
        tof_output_path,
        launch_point_r,
        d_comp,
        r_comp,
    }
}

fn get_arg(arg: &str) -> String {
    let args: Vec<String> = env::args().collect();

    let i = 0;
    loop {
        if i >= args.len() {
            return "".to_string();
        }

        if args[i] == *arg && i + 1 < args.len() {
            return args[i + 1].clone();
        }
    }
}

fn simulate(robot_x: f64, robot_vx: f64, angle_degs: f64, flywheel_speed: f64, internal_config: InternalConfig) -> Trajectory {
    

    let angle_rads = angle_degs.to_radians();
    let angle_unit_vector = Vec2::new(angle_rads.sin(), angle_rads.cos());
    let launch_vector = angle_unit_vector * get_ball_exit_velo(flywheel_speed, &internal_config.config) + Vec2::new(robot_vx, 0.0);

    let obj = Fuel::new(find_launch_pos(robot_x, angle_degs, internal_config), launch_vector);

    while !obj.dead {
        obj.update(0.01);
    }

    return Trajectory {
        initX: robot_x,
        initVX: robot_vx,
        initTheta: angle_degs,
        initVFly: flywheel_speed,

        madeIt: obj.made_it,
        maxHeight: obj.max_height,
        landingX: obj.end.x,
        landingY: obj.end.y,
        tof: obj.tof
    }
}

fn binary_search(robot_x: f64, robot_vx: f64, angle_degs: f64, internal_config: InternalConfig) -> Option<Trajectory> {
    let config = &internal_config.config;
    let mut pivot: f64;
    let mut current_max_speed = config.maxVFly;
    let mut current_min_speed = config.minVFly;
    let mut i = 0;
    let mut successful = false;

    let mut most_recent_traj = None;

    while !successful && i < config.vFlyMaxTries {
        pivot = current_min_speed + (current_max_speed - current_min_speed) / 2.0;

        let traj: Trajectory = simulate(robot_x, robot_vx, angle_degs, pivot, internal_config.clone());
        i = i + 1;
        if traj.landingX < 0.0 {
            current_min_speed = pivot;
        } else {
            current_max_speed = pivot;
        }

        successful = traj.madeIt;
        most_recent_traj = Some(traj);
    }
    if successful {
        return most_recent_traj;
    } else {
        return None;
    }
}

fn evaluate_trajectories(trajectories: &Vec<Option<Trajectory>>, internal_config: InternalConfig) -> Option<Trajectory> {
    let config = &internal_config.config;
    let mut lowest_score = std::f64::MAX;
    let mut best: Option<Trajectory> = None;
    for i in 0..trajectories.len() {
        if let Some(trajectory) = trajectories[i] {
            let trajectory2 = simulate(trajectory.initX, trajectory.initVX, trajectory.initTheta + config.angleDev, trajectory.initVFly + config.vFlyDev, internal_config.clone());
            let dx = trajectory2.landingX - trajectory.landingX;
            let robustness_score = ((dx / config.vFlyDev).powi(2) + (dx / config.angleDev).powi(2)) * config.robustnessFactor;
            
            let height_score = trajectory.maxHeight * config.heightFactor;

            let total_score = robustness_score + height_score;
            if total_score < lowest_score {
                lowest_score = total_score;
                best = Some(trajectory);
            }
        }
    }

    return best;
}

pub fn get_ball_exit_velo(v_fly: f64, config: &ShooterConfig) -> f64{
    return (v_fly + v_fly * config.fVelo) / 2.0;
}

pub fn find_launch_pos(robot_x: f64, angle_degs: f64, internal_config: InternalConfig) -> Vec2 {
    let shooter_pos = Vec2::new(-robot_x, internal_config.config.shooterHeight);
    let ball_relative_to_shooter = Vec2::new(angle_degs.to_radians().cos(), angle_degs.to_radians().sin()) * internal_config.launch_point_r;
    return shooter_pos + ball_relative_to_shooter;
}

#[derive(Clone)]
struct InternalConfig {
    config: ShooterConfig,
    data_input_path: String,
    hood_output_path: String,
    flywheel_output_path: String,
    tof_output_path: String,
    launch_point_r: f64,
    d_comp: f64,
    r_comp: f64,    
}

//For JSON reasons: DO NOT CHANGE THE FOLLOWING STRUCT(S) TO SNAKE CASE!!!
#[derive(Copy, Clone)]
#[derive(Deserialize, Debug)]
struct Trajectory {
    initX: f64,
    initVX: f64,
    initTheta: f64,
    initVFly: f64,

    madeIt: bool,
    maxHeight: f64,
    landingX: f64,
    landingY: f64,

    tof: f64,
}

#[derive(Clone)]
#[derive(Deserialize, Debug)]
struct ShooterConfig {
    shooterHeight: f64,

    rFly: f64,
    rRol: f64,
    rHood: f64,
    fVelo: f64,

    maxVFly: f64,
    minVFly: f64,
    vFlyMaxTries: u32,

    minAngle: f64,
    maxAngle: f64,
    angleRes: u32,

    minVX: f64,
    maxVX: f64,
    vxRes: u32,

    maxX: f64,
    minX: f64,
    xRes: u32,

    angleDev: f64,
    vFlyDev: f64,

    robustnessFactor: f64,
    heightFactor: f64
}