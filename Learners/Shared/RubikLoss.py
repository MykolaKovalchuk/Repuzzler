from keras import backend as K
from keras.utils.generic_utils import get_custom_objects


class CubePoints(object):
    def __init__(self, y):
        self.points = []
        self.points.append((y[:, 0], y[:, 1]))
        self.points.append((y[:, 2], y[:, 3]))
        self.points.append((y[:, 4], y[:, 5]))
        self.points.append((y[:, 6], y[:, 7]))
        self.points.append((y[:, 8], y[:, 9]))
        self.points.append((y[:, 10], y[:, 11]))
        self.points.append((y[:, 12], y[:, 13]))

        self.edges = []
        self.edges.append(self.calc_len(0, 1))
        self.edges.append(self.calc_len(0, 3))
        self.edges.append(self.calc_len(0, 5))
        self.edges.append(self.calc_len(1, 2))
        self.edges.append(self.calc_len(2, 3))
        self.edges.append(self.calc_len(3, 4))
        self.edges.append(self.calc_len(4, 5))
        self.edges.append(self.calc_len(5, 6))
        self.edges.append(self.calc_len(6, 1))

    def calc_len(self, point1, point2):
        x1, y1 = self.points[point1]
        x2, y2 = self.points[point2]
        return K.sqrt(K.square(x1 - x2) + K.square(y1 - y2))


def cube_loss(y_true, y_pred):
    cube_true = CubePoints(y_true)
    cube_pred = CubePoints(y_pred)

    def calc_diff_square(point1, point2):
        x1, y1 = point1
        x2, y2 = point2
        return K.square(x1 - x2) + K.square(y1 - y2)

    l1 = calc_diff_square(cube_true.points[0], cube_pred.points[0])
    for i in range(1, len(cube_true.points)):
        l1 = l1 + calc_diff_square(cube_true.points[i], cube_pred.points[i])
    l1 = K.sqrt(l1)

    l2 = K.square(K.sqrt(cube_true.edges[0]) - K.sqrt(cube_pred.edges[0]))
    for i in range(1, len(cube_true.edges)):
        l2 = l2 + K.square(K.sqrt(cube_true.edges[i]) - K.sqrt(cube_pred.edges[i]))
    l2 = K.sqrt(l2)

    l = l1 + l2
    l = K.reshape(l, (-1, 1))

    return l


def cube_loss2(y_true, y_pred):
    cube_true = CubePoints(y_true)
    cube_pred = CubePoints(y_pred)

    def calc_diff_relative(point_index, edge_indexes):
        total_edge = cube_true.edges[edge_indexes[0]]
        for ei in range(1, len(edge_indexes)):
            total_edge = total_edge + cube_true.edges[edge_indexes[ei]]

        x1, y1 = cube_true.points[point_index]
        x2, y2 = cube_pred.points[point_index]
        point_diff = K.abs(x1 - x2) + K.abs(y1 - y2)

        return point_diff * len(edge_indexes) / total_edge

    l1 = calc_diff_relative(0, [0, 1, 2]) + \
         calc_diff_relative(1, [0, 3, 8]) + \
         calc_diff_relative(2, [3, 4]) + \
         calc_diff_relative(3, [1, 4, 5]) + \
         calc_diff_relative(4, [5, 6]) + \
         calc_diff_relative(5, [2, 6, 7]) + \
         calc_diff_relative(6, [7, 8])
    l1 = K.sqrt(l1)

    l2 = K.abs(cube_true.edges[0] - cube_pred.edges[0]) / cube_true.edges[0]
    for i in range(1, len(cube_true.edges)):
        l2 = l2 + K.abs(cube_true.edges[i] - cube_pred.edges[i]) / cube_true.edges[i]
    l2 = K.sqrt(l2)

    l = l1 + l2
    l = K.reshape(l, (-1, 1))

    return l


def cube_loss3(y_true, y_pred):
    l1 = cube_loss(y_true, y_pred)
    l2 = cube_loss2(y_true, y_pred)
    return l1 + l2 / 40


def register_losses():
    get_custom_objects().update({"cube_loss": cube_loss})
    get_custom_objects().update({"cube_loss2": cube_loss2})
    get_custom_objects().update({"cube_loss3": cube_loss3})


def correct_label_orientation(label):
    # Rubik 7 points:
    #     4
    #  3     5
    #  |  0  |
    #  2  |  6
    #     1
    # Edges: 0-1, 0-3, 0-5, 1-2, 2-3, 3-4, 4-5, 5-6
    if label[3][1] > label[1][1] or label[5][1] > label[1][1]:
        if label[5][1] > label[3][1]:
            label = rotate_label(label, [(1, 5, 3), (2, 6, 4)])
        else:
            label = rotate_label(label, [(1, 3, 5), (2, 4, 6)])
    return label


def rotate_label(label, groups):
    for group in groups:
        ta, tb = label[group[0]]  # use elements to prevent reference-copy arrays
        for i in range(len(group) - 1):
            label[group[i]] = label[group[i + 1]]
        label[group[-1]] = (ta, tb)
    return label


def get_horizontal_flip_pairs():
    # Points to switch on horizontal flip:
    #     4
    #  3=====5
    #     0
    #  2=====6
    #     1
    return [(2, 6), (3, 5)]
