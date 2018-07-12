from keras.utils import Sequence
from DataLoader import DataLoader
import numpy as np
import cv2
import random


def fi(p):
    return int(p[0]), int(p[1])


class DataGenerator(Sequence):
    def __init__(self, images_folder, labels_folder,
                 batch_size=16,
                 images_extension=".png", labels_extension=".bounds",
                 target_width=299, target_height=299):
        self.batchSize = batch_size
        self.targetWidth = target_width
        self.targetHeight = target_height

        self.loader = DataLoader(images_folder, labels_folder,
                                 images_extension, labels_extension)
        self.image_files = self.loader.get_image_file_names()
        self.images_count = len(self.image_files)

        self.flip_label_pairs = []

        random.seed()

    def __len__(self):
        return int(np.ceil(self.images_count / float(self.batchSize)))

    def __getitem__(self, idx):
        return self.get_item(idx, True)

    def get_item(self, idx, augment):
        start_index = idx * self.batchSize % self.images_count
        last_index = min(start_index + self.batchSize, self.images_count)

        batch_images = []
        batch_labels = []

        for index in range(start_index, last_index):
            image, label = self.loader.load_image_and_labels(self.image_files[index])

            if augment:
                image, label = self.augment_item(image, label)

            label = self.correct_label_orientation(label)

            image_height = image.shape[0]
            image_width = image.shape[1]
            scale = max(self.targetHeight / image_height, self.targetWidth / image_width)
            if augment:
                scale *= 0.8 + random.random() * 0.4
            image, label = self.scale_image(image, label, scale)

            image_height = image.shape[0]
            image_width = image.shape[1]
            dx = image_width - self.targetWidth
            dy = image_height - self.targetHeight
            if augment:
                dx *= random.random()
                dy *= random.random()
                for x, y in label:
                    if 0 <= x < dx:
                        dx = x
                    if dx + self.targetWidth < x < image_width:
                        dx = x - self.targetWidth
                    if 0 <= y < dy:
                        dy = y
                    if dy + self.targetHeight < y < image_height:
                        dy = y - self.targetHeight
                shift = (dx, dy)
            else:
                shift = (dx / 2, dy / 2)
            image, label = self.cut_image(image, label, shift)

            for li in range(0, len(label)):
                x, y = label[li]
                x /= float(self.targetWidth)
                y /= float(self.targetHeight)
                label[li] = (x, y)

            batch_images.append(image)
            batch_labels.append(self.flatten_label(label))

        return np.array(batch_images), np.array(batch_labels)

    def correct_label_orientation(self, label):
        # hardcoded for Rubik 7 points:
        #     4
        #  3     5
        #     0
        #  2     6
        #     1
        if label[3][1] > label[1][1] or label[5][1] > label[1][1]:
            if label[5][1] > label[3][1]:
                label = self.rotate_label(label, [(1, 5, 3), (2, 6, 4)])
            else:
                label = self.rotate_label(label, [(1, 3, 5), (2, 4, 6)])
        return label

    def rotate_label(self, label, groups):
        for group in groups:
            t = label[group[0]]
            for i in range(len(group) - 1):
                label[group[i]] = label[group[i + 1]]
            label[group[-1]] = t
        return label

    def flatten_label(self, label):
        fl = []
        for x, y in label:
            fl.append(x)
            fl.append(y)
        return fl

    def augment_item(self, image, label):
        if random.random() > 0.5:
            image, label = self.flip_image_lr(image, label)

        return image, label

    def flip_image_lr(self, image, label):
        image = cv2.flip(image, 1)

        image_width = image.shape[1]
        for li in range(len(label)):
            x, y = label[li]
            label[li] = image_width - x, y

        for a, b in self.flip_label_pairs:
            t = label[a]
            label[a] = label[b]
            label[b] = t

        return image, label

    def scale_image(self, image, label, scale):
        image_height = image.shape[0]
        image_width = image.shape[1]
        new_height = int(image_height * scale + 0.5)
        new_width = int(image_width * scale + 0.5)

        image = cv2.resize(image, (new_width, new_height))

        for li in range(len(label)):
            x, y = label[li]
            x *= new_width / image_width
            y *= new_height / image_height
            label[li] = (x, y)

        return image, label

    def cut_image(self, image, label, shift):
        image_height = image.shape[0]
        image_width = image.shape[1]

        x0, y0 = int(shift[0] + 0.5), int(shift[1] + 0.5)
        for li in range(len(label)):
            x, y = label[li]
            x -= x0
            y -= y0
            label[li] = (x, y)

        sx0 = x0 if x0 >= 0 else 0
        sy0 = y0 if y0 >= 0 else 0
        sx1 = min(x0 + self.targetWidth, image_width)
        sy1 = min(y0 + self.targetHeight, image_height)

        tx0 = 0 if x0 >= 0 else -x0
        ty0 = 0 if y0 >= 0 else -y0
        tx1 = tx0 + sx1 - sx0
        ty1 = ty0 + sy1 - sy0

        result = np.zeros((self.targetHeight, self.targetWidth, 3), dtype=np.uint8)
        result[ty0:ty1, tx0:tx1, :] = image[sy0:sy1, sx0:sx1, :]

        return result, label
