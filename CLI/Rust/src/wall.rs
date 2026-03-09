use crate::vec2::Vec2;
use lazy_static::lazy_static;

lazy_static! {
    pub static ref FLOOR: Wall = Wall::new(Vec2::new(200.0, 0.0), Vec2::new(-200.0, 0.0));
    pub static ref HUB_ORIGIN: Vec2 = Vec2::new(0.0, 1.8288);
    pub static ref HUB: Vec<Wall> = Wall::create_rect(1.688288, 1.383801, Vec2::new(0.0, -1.1368) + *HUB_ORIGIN);
    pub static ref HUB_TOP: Wall = Wall::new(Vec2::new(-1.06/2.0, 0.0) + *HUB_ORIGIN, Vec2::new(1.06/2.0, 0.0) + *HUB_ORIGIN);
    pub static ref HUB_BOTTOM: Wall = Wall::new(Vec2::new(-0.605/2.0, -0.39) + *HUB_ORIGIN, Vec2::new(0.605/2.0, -0.39) + *HUB_ORIGIN);
    pub static ref HUB_SIDE_INNER: Wall = Wall::from_origin(Vec2::new(-0.415, -0.2) + *HUB_ORIGIN, 0.47, 30.0);
    pub static ref HUB_SIDE_OUTER: Wall = Wall::from_origin(Vec2::new(0.415, -0.2) + *HUB_ORIGIN, 0.47, -30.0);

    pub static ref ALL_KILL_WALLS: Vec<Wall> = vec![
        *FLOOR,
        HUB[0], HUB[1], HUB[2], HUB[3],
        *HUB_SIDE_INNER,
    ];

    pub static ref HUB_SIDES: Vec<Wall> = vec![
        *HUB_SIDE_OUTER
    ];
}

#[derive(Copy, Clone)]
pub struct Wall {
    pub left_point: Vec2,
    pub right_point: Vec2,
    pub origin: Vec2,
    pub orthogonal: Vec2,
    pub reverse_orthogonal: Vec2,
    pub inline: Vec2,
    pub length: f64
}

impl Wall {
    pub fn new(left_point: Vec2, right_point: Vec2) -> Self {
        let inline = (left_point - right_point).normalize();
        let orthogonal = Vec2::new(-inline.y, inline.x);
        Self {
            left_point,
            right_point,
            origin: (left_point + right_point) / 2.0,
            orthogonal,
            reverse_orthogonal: -orthogonal,
            inline,
            length: (right_point - left_point).norm()
        }
    }

    pub fn from_origin(origin: Vec2, length: f64, angle_degrees: f64) -> Self {
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
            length
        }
    }

    pub fn create_rect(width: f64, height: f64, center: Vec2) -> Vec<Self> {
        let side1 = Self::new(center + Vec2::new(-width / 2.0, height / 2.0), center + Vec2::new(-width / 2.0, -height / 2.0));
        let side2 = Self::new(center + Vec2::new(width / 2.0, -height / 2.0), center + Vec2::new(width / 2.0, height / 2.0));
        let side3 = Self::new(center + Vec2::new(width / 2.0, height / 2.0), center + Vec2::new(-width / 2.0, height / 2.0));
        let side4 = Self::new(center + Vec2::new(-width / 2.0, -height / 2.0), center + Vec2::new(width / 2.0, -height / 2.0));
        return vec![side1, side2, side3, side4];
    }
}