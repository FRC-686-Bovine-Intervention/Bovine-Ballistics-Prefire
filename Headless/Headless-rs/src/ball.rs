use crate::vec2::{self, Vec2};

pub struct Ball {
    pub previous_position: Vec2,
    pub position: Vec2,
    radius: f64
}

impl Ball {
    pub fn new(radius: f64) -> Self {
        Ball {
            previous_position: *vec2::ZERO,
            position: *vec2::ZERO,
            radius
        }
    }

    pub fn has_hit(&self, wall: Wall, act_on_both_sides: bool) -> bool {
        let to_wall_origin_previous = wall.origin - self.previous_position;
        let distance_to_wall_previous = to_wall_origin_previous.dot(wall.orthogonal) - self.radius;
        let to_wall_origin = wall.origin - self.position;
        let distance_to_wall = to_wall_origin.dot(wall.orthogonal) - self.radius;

        let inline_distance_previous = to_wall_origin_previous.dot(wall.inline);
        let inline_distance = to_wall_origin.dot(wall.inline);

        if distance_to_wall <= 0.0 && distance_to_wall_previous > 0.0 && inline_distance.abs() < wall.length / 2.0 {
            return true;
        }

        if act_on_both_sides {
            if distance_to_wall >= 0.0 && distance_to_wall_previous < 0.0 && inline_distance.abs() < wall.length / 2.0 {
                return true;
            }
        }

        return false;
    }

    pub fn has_hit_reverse(&self, wall: Wall) -> bool {
        let to_wall_origin_previous = wall.origin - self.previous_position;
        let distance_to_wall_previous = to_wall_origin_previous.dot(wall.orthogonal) - self.radius;
        let to_wall_origin = wall.origin - self.position;
        let distance_to_wall = to_wall_origin.dot(wall.orthogonal) - self.radius;

        let inline_distance_previous = to_wall_origin_previous.dot(wall.inline);
        let inline_distance = to_wall_origin.dot(wall.inline);

        if distance_to_wall >= 0.0 && distance_to_wall_previous < 0.0 && inline_distance.abs() < wall.length / 2.0 {
            return true;
        }
        return false;
    }

    pub fn has_hit_any(&self, walls: Vec<Wall>, act_on_both_sides: bool) -> bool {
        for wall in walls {
            if self.has_hit(wall, act_on_both_sides) {
                return true;
            }
        }
        return false;
    }

    pub fn has_hit_any_reverse(&self, walls: Vec<Wall>) -> bool {
        for wall in walls {
            if self.has_hit_reverse(wall) {
                return true;
            }
        }
        return false;
    }

    // pub fn new(init_pos: Vec2, radius: f64) -> Self {
    //     Ball {
    //         previous_position: vec2::ZERO,
    //         position: init_pos,
    //         radius
    //     }
    // }
}