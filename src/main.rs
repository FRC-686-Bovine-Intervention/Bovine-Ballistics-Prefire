use std::env;

use nalgebra::{DMatrix, DVector, SVD};
use rayon::iter::{IntoParallelRefIterator, ParallelIterator};
use serde::{Deserialize, Serialize};

use crate::{projectile::{EasyEnvironment, Environment, FlyingProjectile, Projectile}, vec2::Vec2};

mod vec2;
mod wall;
mod projectile;


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

    let hood_polynomial = fit_two_variable_3rd_degree(&best_trajectories, |traj| traj.initTheta.to_radians(), 0.0);
    let flywheel_polynomial = fit_two_variable_3rd_degree(&best_trajectories, |traj| traj.initVFly, 0.0);
    let tof_polynomial = fit_two_variable_3rd_degree(&best_trajectories, |traj| traj.tof, 0.0);

    let hood_json = serde_json::to_string_pretty(&hood_polynomial).expect("Serialization failed");
    std::fs::write(internal_config.hood_output_path, hood_json).expect("Failed to write file for some reason");
    let flywheel_json = serde_json::to_string_pretty(&flywheel_polynomial).expect("Serialization failed");
    std::fs::write(internal_config.flywheel_output_path, flywheel_json).expect("Failed to write file for some reason");
    let tof_json = serde_json::to_string_pretty(&tof_polynomial).expect("Serialization failed");
    std::fs::write(internal_config.tof_output_path, tof_json).expect("Failed to write file for some reason");

    let mut total_error: f64 = 0.0;
    for i in 0..best_trajectories.len() {
        let predicted = hood_polynomial.evaluate(
            best_trajectories[i].initX,
            best_trajectories[i].initVX
        );

        let actual = best_trajectories[i].initTheta.to_radians();

        let err = predicted - actual;
        total_error += err * err;
    }

    println!("RMSE: {}", (total_error/(best_trajectories.len() as f64)).sqrt());
    println!("Finished");
}

fn awake() -> InternalConfig {
    let mut data_input_path = "userconfig.json".to_string();
    let mut game_input_path = "gameconfig.json".to_string();

    if get_arg("--configpath") != "" {
        data_input_path = get_arg("--configpath");
    }

    if get_arg("--gamepath") != "" {
        game_input_path = get_arg("--gamepath");
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
    println!("{}", &json);
    let config: ShooterConfig = serde_json::from_str(&json).unwrap();
    println!("Successfully got user JSON");

    let d_comp: f64 = config.rHood - config.rRol - config.rFly;
    let r_comp: f64 = d_comp / 2.0;
    let launch_point_r = r_comp + config.rFly; // derived from r_comp, no duplication

    let json = std::fs::read_to_string(&game_input_path).unwrap();
    println!("{}", &json);
    let gameconfig: GameConfig = serde_json::from_str(&json).unwrap();
    println!("Successfully got game JSON");

    InternalConfig {
        config,
        gameconfig,
        data_input_path,
        game_input_path,
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

    for i in 0..args.len() {
        if args[i] == arg && i + 1 < args.len() {
            return args[i + 1].clone();
        }
    }

    "".to_string()
}

fn simulate(robot_x: f64, robot_vx: f64, angle_degs: f64, flywheel_speed: f64, internal_config: InternalConfig) -> Trajectory {
    

    let angle_rads = angle_degs.to_radians();
    let angle_unit_vector = Vec2::new(angle_rads.sin(), angle_rads.cos());
    let launch_vector = angle_unit_vector * get_ball_exit_velo(flywheel_speed, &internal_config.config) + Vec2::new(robot_vx, 0.0);

    let mut obj = FlyingProjectile::new(internal_config.gameconfig.projectile, Environment::from_easy_environment(internal_config.gameconfig.environment.clone()),find_launch_pos(robot_x, angle_degs, internal_config), launch_vector);

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

fn get_ball_exit_velo(v_fly: f64, config: &ShooterConfig) -> f64{
    return (v_fly + v_fly * config.fVelo) / 2.0;
}

fn find_launch_pos(robot_x: f64, angle_degs: f64, internal_config: InternalConfig) -> Vec2 {
    let shooter_pos = Vec2::new(-robot_x, internal_config.config.shooterHeight);
    let ball_relative_to_shooter = Vec2::new(angle_degs.to_radians().cos(), angle_degs.to_radians().sin()) * internal_config.launch_point_r;
    return shooter_pos + ball_relative_to_shooter;
}

fn fit_two_variable_3rd_degree<F>(
    trajectories: &Vec<Trajectory>,
    y_selector: F,
    ridge_lambda: f64
) -> TwoVariablePolynomial3rdDegree where F: Fn(&Trajectory) -> f64 {
    let n = trajectories.len();
    const M: usize = 10;

    if n < M {
        eprintln!("Warning: only {n} samples but {M} coefficients");
    }

     let x_mean = trajectories.iter().map(|t| t.initX).sum::<f64>() / n as f64;
    let y_mean = trajectories.iter().map(|t| t.initVX).sum::<f64>() / n as f64;
    let z_mean = trajectories.iter().map(|t| y_selector(t)).sum::<f64>() / n as f64;

    let mut x_scale = (trajectories.iter().map(|t| (t.initX - x_mean).powi(2)).sum::<f64>() / n as f64).sqrt();
    let mut y_scale = (trajectories.iter().map(|t| (t.initVX - y_mean).powi(2)).sum::<f64>() / n as f64).sqrt();
    let mut z_scale = (trajectories.iter().map(|t| (y_selector(t) - z_mean).powi(2)).sum::<f64>() / n as f64).sqrt();

    if x_scale == 0.0 { x_scale = 1.0; }
    if y_scale == 0.0 { y_scale = 1.0; }
    if z_scale == 0.0 { z_scale = 1.0; }

    let mut a_data = vec![0.0f64; n * M];
    let mut b_data = vec![0.0f64; n];

    for (i, traj) in trajectories.iter().enumerate() {
        let x1 = (traj.initX - x_mean) / x_scale;
        let x2 = (traj.initVX - y_mean) / y_scale;
        let z = (y_selector(traj) - z_mean) / z_scale;

        a_data[i * M + 0] = 1.0;
        a_data[i * M + 1] = x1;
        a_data[i * M + 2] = x2;
        a_data[i * M + 3] = x1 * x1;
        a_data[i * M + 4] = x1 * x2;
        a_data[i * M + 5] = x2 * x2;
        a_data[i * M + 6] = x1 * x1 * x1;
        a_data[i * M + 7] = x1 * x1 * x2;
        a_data[i * M + 8] = x1 * x2 * x2;
        a_data[i * M + 9] = x2 * x2 * x2;

        b_data[i] = z;
    }

    let a = DMatrix::from_row_slice(n, M, &a_data);
    let b = DVector::from_vec(b_data);

    let svd = SVD::new(a.clone(), true, true);
    let singular_values = &svd.singular_values;
    let condition = singular_values[0] / singular_values[singular_values.len() - 1];
    println!("Condition estimate: {condition:E}");

    let coeffs = if ridge_lambda <= 0.0 {
        svd.solve(&b, 1e-12).expect("SVD solve failed")
    } else {
        let at_a = a.transpose() * &a;
        let mut at_a_reg = at_a;
        for j in 0..M {
            at_a_reg[(j, j)] += ridge_lambda;
        }
        let at_b = a.transpose() * &b;
        at_a_reg.lu().solve(&at_b).expect("Ridge solve failed")
    };

    TwoVariablePolynomial3rdDegree {
        coefficients: coeffs.as_slice().to_vec(),
        xMean: x_mean,
        yMean: y_mean,
        zMean: z_mean,
        xScale: x_scale,
        yScale: y_scale,
        zScale: z_scale,
    }
}

#[derive(Clone)]
struct InternalConfig {
    config: ShooterConfig,
    gameconfig: GameConfig,
    data_input_path: String,
    game_input_path: String,
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
    eventCode: String,

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

#[derive(Clone)]
#[derive(Deserialize, Debug)]
struct GameConfig {
    projectile: Projectile,
    environment: EasyEnvironment,
}

#[derive(Debug, Clone, Serialize)]
pub struct TwoVariablePolynomial3rdDegree {
    pub coefficients: Vec<f64>,
    pub xMean: f64,
    pub yMean: f64,
    pub zMean: f64,
    pub xScale: f64,
    pub yScale: f64,
    pub zScale: f64,
}

impl TwoVariablePolynomial3rdDegree {
    pub fn evaluate(&self, x: f64, vx: f64) -> f64 {
        let x1 = (x - self.xMean) / self.xScale;
        let x2 = (vx - self.yMean) / self.yScale;
        let c = &self.coefficients;
        let z_norm = c[0]
            + c[1] * x1
            + c[2] * x2
            + c[3] * x1 * x1
            + c[4] * x1 * x2
            + c[5] * x2 * x2
            + c[6] * x1 * x1 * x1
            + c[7] * x1 * x1 * x2
            + c[8] * x1 * x2 * x2
            + c[9] * x2 * x2 * x2;
        z_norm * self.zScale + self.zMean
    }
}
