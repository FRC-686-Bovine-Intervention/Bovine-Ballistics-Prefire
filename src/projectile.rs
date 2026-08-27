use serde::Deserialize;

use crate::{vec2::Vec2, wall::{EasyWall, Wall}};

#[derive(Copy, Clone)]
#[derive(Deserialize, Debug)]
pub struct Projectile {
    pub r: f64,
    pub area: f64,
    pub resistance_coefficient: f64,
    pub magnus_coefficient: f64,
    pub mass: f64
}

#[derive(Clone)]
#[derive(Deserialize, Debug)]
pub struct Environment {
    pub air_density: f64,
    pub g: f64,
    pub success_walls: Vec<Wall>,
    pub failure_walls: Vec<Wall>
}

#[derive(Clone)]
#[derive(Deserialize, Debug)]
pub struct EasyEnvironment {
    pub air_density: f64,
    pub g: f64,
    pub success_walls: Vec<EasyWall>,
    pub failure_walls: Vec<EasyWall>
}

impl Environment {
    pub fn from_easy_environment(easy_environment: EasyEnvironment) -> Self {
        let success_walls = easy_environment.success_walls.into_iter().map(|easy_wall| Wall::from_easy_wall(easy_wall)).collect();
        let failure_walls = easy_environment.failure_walls.into_iter().map(|easy_wall| Wall::from_easy_wall(easy_wall)).collect();
        Self {
            air_density: easy_environment.air_density,
            g: easy_environment.g,
            success_walls,
            failure_walls
        }
    }
}

#[derive(Clone)]
pub struct FlyingProjectile {
    pub projectile: Projectile,
    pub environment: Environment,
    pub p: Vec2,
    pub v: Vec2,
    previous_p: Vec2,
    pub initial_p: Vec2,
    pub initial_v: Vec2,
    pub end: Vec2,
    pub max_height: f64,
    pub tof: f64,
    simulating: bool,
    pub made_it: bool,
    pub dead: bool
}

impl FlyingProjectile {
    pub fn new(projectile: Projectile, environment: Environment, p: Vec2, v: Vec2) -> Self {
        Self {
            projectile,
            environment,
            p,
            v,
            previous_p: p,
            initial_p: p,
            initial_v: v,
            end: p,
            max_height: 0.0,
            tof: 0.0,
            simulating: true,
            made_it: false,
            dead: false
        }
    }

    pub fn update(&mut self, delta_time: f64) {
        if !self.simulating {return;}
        self.previous_p = self.p;

        if self.p.y > self.max_height {
            self.max_height = self.p.y;
        }

        let mut total_forces = Vec2::new(0.0, -self.environment.g * self.projectile.mass);

        let speed = self.v.norm();
        if speed > 0.0 {
            let drag_force = -0.5 * self.environment.air_density * speed * self.projectile.resistance_coefficient * self.projectile.area * self.v;
            total_forces += drag_force;
        }

        let a = total_forces / self.projectile.mass;

        self.v += a * delta_time;
        self.previous_p = self.p;
        self.p += self.v * delta_time;

        self.tof += delta_time;

        if self.has_hit_success_walls() {
            self.dead = true;
            self.made_it = true;
            self.end = self.p;
        } else if self.has_hit_failure_walls() {
            self.dead = true;
            self.made_it = false;
            self.end = self.p;
        }
    }

    fn has_hit_success_walls(&self) -> bool {
        return self.has_hit_walls(&self.environment.success_walls);
    }

    fn has_hit_failure_walls(&self) -> bool {
        return self.has_hit_walls(&self.environment.failure_walls);
    }

    fn has_hit_walls(&self, walls: &Vec<Wall>) -> bool {
        for wall in walls {
            if self.has_hit_wall(&wall) {
                return true;
            }
        }
        
        return false;
    }

    fn has_hit_wall(&self, wall: &Wall) -> bool {
        let to_wall_origin_previous = wall.origin - self.previous_p;
        let distance_to_wall_previous = to_wall_origin_previous.dot(wall.orthogonal) - self.projectile.r;
        
        let to_wall_origin = wall.origin - self.p;
        let distance_to_wall = to_wall_origin.dot(wall.orthogonal) - self.projectile.r;

        let inline_distance_previous = to_wall_origin_previous.dot(wall.inline);
        
        let inline_distance = to_wall_origin.dot(wall.inline);

        if distance_to_wall <= 0.0 && distance_to_wall_previous > 0.0 && inline_distance.abs() < wall.length / 2.0 {
            return true;
        }

        if wall.works_reverse {
            if distance_to_wall >= 0.0 && distance_to_wall_previous < 0.0 && inline_distance.abs() < wall.length / 2.0 {
                return true;
            }
        }

        return false;
    }
}