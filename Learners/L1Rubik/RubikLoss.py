from keras import backend as K


class CubePoints(object):
    def __init__(self, y):
        self.x0 = y[:, 0]
        self.y0 = y[:, 1]
        self.x1 = y[:, 2]
        self.y1 = y[:, 3]
        self.x2 = y[:, 4]
        self.y2 = y[:, 5]
        self.x3 = y[:, 6]
        self.y3 = y[:, 7]
        self.x4 = y[:, 8]
        self.y4 = y[:, 9]
        self.x5 = y[:, 10]
        self.y5 = y[:, 11]
        self.x6 = y[:, 12]
        self.y6 = y[:, 13]

        self.w0 = K.sqrt(K.square(self.x0 - self.x1) + K.square(self.y0 - self.y1))
        self.w1 = K.sqrt(K.square(self.x0 - self.x3) + K.square(self.y0 - self.y3))
        self.w2 = K.sqrt(K.square(self.x0 - self.x5) + K.square(self.y0 - self.y5))
        self.w3 = K.sqrt(K.square(self.x1 - self.x2) + K.square(self.y1 - self.y2))
        self.w4 = K.sqrt(K.square(self.x2 - self.x3) + K.square(self.y2 - self.y3))
        self.w5 = K.sqrt(K.square(self.x3 - self.x4) + K.square(self.y3 - self.y4))
        self.w6 = K.sqrt(K.square(self.x4 - self.x5) + K.square(self.y4 - self.y5))
        self.w7 = K.sqrt(K.square(self.x5 - self.x6) + K.square(self.y5 - self.y6))
        self.w8 = K.sqrt(K.square(self.x6 - self.x1) + K.square(self.y6 - self.y1))


def cube_loss(y_true, y_pred):
    cube_true = CubePoints(y_true)
    cube_pred = CubePoints(y_pred)

    l1 = K.square(cube_true.x0 - cube_pred.x0) + K.square(cube_true.y0 - cube_pred.y0) + \
         K.square(cube_true.x1 - cube_pred.x1) + K.square(cube_true.y1 - cube_pred.y1) + \
         K.square(cube_true.x2 - cube_pred.x2) + K.square(cube_true.y2 - cube_pred.y2) + \
         K.square(cube_true.x3 - cube_pred.x3) + K.square(cube_true.y3 - cube_pred.y3) + \
         K.square(cube_true.x4 - cube_pred.x4) + K.square(cube_true.y4 - cube_pred.y4) + \
         K.square(cube_true.x5 - cube_pred.x5) + K.square(cube_true.y5 - cube_pred.y5) + \
         K.square(cube_true.x6 - cube_pred.x6) + K.square(cube_true.y6 - cube_pred.y6)
    l1 = K.sqrt(l1)

    l2 = K.square(K.sqrt(cube_true.w0) - K.sqrt(cube_pred.w0)) + \
         K.square(K.sqrt(cube_true.w1) - K.sqrt(cube_pred.w1)) + \
         K.square(K.sqrt(cube_true.w2) - K.sqrt(cube_pred.w2)) + \
         K.square(K.sqrt(cube_true.w3) - K.sqrt(cube_pred.w3)) + \
         K.square(K.sqrt(cube_true.w4) - K.sqrt(cube_pred.w4)) + \
         K.square(K.sqrt(cube_true.w5) - K.sqrt(cube_pred.w5)) + \
         K.square(K.sqrt(cube_true.w6) - K.sqrt(cube_pred.w6)) + \
         K.square(K.sqrt(cube_true.w7) - K.sqrt(cube_pred.w7)) + \
         K.square(K.sqrt(cube_true.w8) - K.sqrt(cube_pred.w8))
    l2 = K.sqrt(l2)

    l = l1 + l2
    l = K.reshape(l, (-1, 1))

    return l


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
