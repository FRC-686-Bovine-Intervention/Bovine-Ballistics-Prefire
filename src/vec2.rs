use std::ops::{Add, AddAssign, Div, Mul, Neg, Sub};

use serde::Deserialize;

static ZERO: Vec2 = Vec2::new(0.0, 0.0);

#[derive(Copy, Clone)]
#[derive(Deserialize, Debug)]
pub struct Vec2 {
    pub x: f64,
    pub y: f64
}

impl Vec2 {
    pub const fn new(x: f64, y: f64) -> Self {
        return Self {
            x,
            y
        }
    }

    pub fn dot(&self, other: Vec2) -> f64 {
        self.x * other.x + self.y * other.y
    }

    pub fn normalize(&self) -> Self {
        let norm = self.norm();
        let x = self.x / norm;
        let y = self.y / norm;
        return Self {
            x,
            y
        }
    }

    pub fn norm(&self) -> f64 {
        return (self.x.powi(2) + self.y.powi(2)).sqrt();
    }
}

impl Mul<f64> for Vec2 {
    type Output = Vec2;

    fn mul(self, rhs: f64) -> Self::Output {
        Vec2::new(self.x * rhs, self.y * rhs)
    }
}

impl Div<f64> for Vec2 {
    type Output = Self;

    fn div(self, rhs: f64) -> Self::Output {
        Vec2::new(self.x / rhs, self.y / rhs)
    }
}

impl Add for Vec2 {
    type Output = Vec2;

    fn add(self, rhs: Self) -> Self::Output {
        Vec2::new(self.x + rhs.x, self.y + rhs.y)
    }
}

impl Sub for Vec2 {
    type Output = Self;

    fn sub(self, rhs: Self) -> Self::Output {
        Vec2::new(self.x - rhs.x, self.y - rhs.y)
    }
}

impl Neg for Vec2 {
    type Output = Self;

    fn neg(self) -> Self::Output {
        Vec2::new(-self.x, -self.y)
    }
}

impl AddAssign for Vec2 {
    fn add_assign(&mut self, rhs: Self) {
        self.x = self.x + rhs.x;
        self.y = self.y + rhs.y;
    }
}

impl Mul<Vec2> for f64 {
    type Output = Vec2;

    fn mul(self, rhs: Vec2) -> Self::Output {
        rhs * self
    }
}