use std::f64::consts::PI;

use crate::{ball::Ball, vec2::Vec2, wall};

pub struct Fuel {
    r: f64,
    drag_coeff: f64,
    air_density: f64,
    g: f64,

    area: f64,
    mass: f64,

    p: Vec2,
    v: Vec2,
    init_v: Vec2,
    init_p: Vec2,
    pub(crate) end: Vec2,

    pub(crate) max_height: f64,
    pub(crate) tof: f64,

    fuel: Ball,

    simulating: bool,
    pub(crate) made_it: bool,
    pub(crate) dead: bool,
}

impl Fuel {
    pub fn new(pos: Vec2, vel: Vec2) -> Self {
        let r = 0.150114;
        Fuel {
            r,
            drag_coeff: 0.47,
            air_density: 1.225,
            g: 9.81,
            area: PI * r.powi(2),
            mass: 0.226,
            p: pos,
            v: vel,
            init_v: vel,
            init_p: pos,
            end: pos,
            max_height: 0.0,
            tof: 0.0,
            fuel: Ball::new(r),
            simulating: true,
            made_it: false,
            dead: false,
        }
    }

    pub fn update(&mut self, delta_time: f64) {
        if !self.simulating {return;}
        self.fuel.previous_position = self.p;

        if self.p.y > self.max_height {
            self.max_height = self.p.y;
        }

        let mut total_forces = Vec2::new(0.0, -self.g * self.mass);

        let speed = self.v.norm();
        if speed > 0.0 {
            let drag_force = -0.5 * self.air_density * speed * self.drag_coeff * self.area * self.v.normalize();
            total_forces += drag_force;
        }

        let a = total_forces / self.mass;

        self.v += a * delta_time;
        self.p += self.v * delta_time;

        self.fuel.position = self.p;

        self.tof += delta_time;

        if self.fuel.has_hit_any(wall::ALL_KILL_WALLS.clone(), true) || self.fuel.has_hit_any(wall::HUB_SIDES.clone(), false) {
            self.dead = true;
            self.made_it = false;
            self.end = self.fuel.position;
        } else if self.fuel.has_hit(*wall::HUB_TOP, false) {
            self.made_it = true;
        } else if self.fuel.has_hit(*wall::HUB_BOTTOM, false) || self.fuel.has_hit_any_reverse(wall::HUB_SIDES.clone()) {
            self.dead = true;
            self.end = self.fuel.position;
        }
    }
}