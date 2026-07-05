use serde::Deserialize;

use crate::vec2::Vec2;

#[derive(Copy, Clone)]
#[derive(Deserialize, Debug)]
pub struct Wall {
    pub left_point: Vec2,
    pub right_point: Vec2,
    pub origin: Vec2,
    pub orthogonal: Vec2,
    pub reverse_orthogonal: Vec2,
    pub inline: Vec2,
    pub length: f64,
    pub works_reverse: bool,
}

impl Wall {
    pub fn new(left_point: Vec2, right_point: Vec2, works_reverse: bool) -> Self {
        let inline = (left_point - right_point).normalize();
        let orthogonal = Vec2::new(-inline.y, inline.x);
        Self {
            left_point,
            right_point,
            origin: (left_point + right_point) / 2.0,
            orthogonal,
            reverse_orthogonal: -orthogonal,
            inline,
            length: (right_point - left_point).norm(),
            works_reverse
        }
    }

    pub fn from_origin(origin: Vec2, length: f64, angle_degrees: f64, works_reverse: bool) -> Self {
        let left_unit = Vec2::new(angle_degrees.to_radians().cos(), angle_degrees.to_radians().sin());
        let right_unit = -left_unit;

        let left_point = origin + left_unit * length / 2.0;
        let right_point = origin + right_unit * length / 2.0;

        let inline = (left_point - right_point).normalize();
        let orthogonal = Vec2::new(-inline.y, inline.x);

        Self {
            left_point,
            right_point,
            origin,
            orthogonal,
            reverse_orthogonal: -orthogonal,
            inline,
            length,
            works_reverse
        }
    }

    pub fn create_rect(width: f64, height: f64, center: Vec2) -> Vec<Self> {
        let side1 = Self::new(center + Vec2::new(-width / 2.0, height / 2.0), center + Vec2::new(-width / 2.0, -height / 2.0), true);
        let side2 = Self::new(center + Vec2::new(width / 2.0, -height / 2.0), center + Vec2::new(width / 2.0, height / 2.0), true);
        let side3 = Self::new(center + Vec2::new(width / 2.0, height / 2.0), center + Vec2::new(-width / 2.0, height / 2.0), true);
        let side4 = Self::new(center + Vec2::new(-width / 2.0, -height / 2.0), center + Vec2::new(width / 2.0, -height / 2.0), true);
        return vec![side1, side2, side3, side4];
    }
}