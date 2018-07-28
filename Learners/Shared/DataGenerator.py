from DataLoader import DataLoader
from keras.utils import Sequence
import cv2
import gc
import math
import numpy as np
import random


# import Visualizer


class DataGenerator(Sequence):
    def __init__(self, data_loader, images_files, batch_size=16, augment_data=True,
                 target_width=299, target_height=299,
                 label_flip_pairs=[], label_extra_normalization=None):
        self.loader = data_loader
        self.images_files = images_files
        self.images_count = len(self.images_files)

        self.batch_size = batch_size
        self.augment_data = augment_data
        self.target_width = target_width
        self.target_height = target_height

        self.label_flip_pairs = label_flip_pairs
        self.label_extra_normalization = label_extra_normalization

        random.seed()
        random.shuffle(self.images_files)

    @classmethod
    def init_from_folder(cls, images_folder, labels_folder, batch_size=16, augment_data=True,
                         images_extension=".png", labels_extension=".bounds",
                         target_width=299, target_height=299,
                         label_flip_pairs=[], label_extra_normalization=None):

        loader = DataLoader(images_folder, labels_folder, images_extension, labels_extension)
        image_files = loader.get_image_file_names()

        return cls(loader, image_files, batch_size=batch_size, augment_data=augment_data,
                   target_width=target_width, target_height=target_height,
                   label_flip_pairs=label_flip_pairs, label_extra_normalization=label_extra_normalization)

    def get_validation_data(self):
        training_data_count = self.images_count * 7 // 8 // self.batch_size * self.batch_size

        validation_files = self.images_files[training_data_count:]
        self.images_files = self.images_files[:training_data_count]
        self.images_count = len(self.images_files)

        return DataGenerator(self.loader, validation_files, batch_size=self.batch_size, augment_data=False,
                             target_width=self.target_width, target_height=self.target_height,
                             label_flip_pairs=self.label_flip_pairs,
                             label_extra_normalization=self.label_extra_normalization)

    def __len__(self):
        return int(np.ceil(self.images_count / float(self.batch_size)))

    def on_epoch_end(self):
        random.shuffle(self.images_files)

    def __getitem__(self, idx):
        gc.collect()

        start_index = idx * self.batch_size % self.images_count
        last_index = min(start_index + self.batch_size, self.images_count)

        batch_images = []
        batch_labels = []

        for index in range(start_index, last_index):
            image, label = self.loader.load_image_and_labels(self.images_files[index])
            image, label = self.augment_item(image, label)

            label = self.normalize_label(label)

            # Visualizer.show_image(image, label, title=self.images_files[index])  # check augmentation result
            image = image / 255.0

            batch_images.append(image)
            batch_labels.append(label)

        return np.array(batch_images), np.array(batch_labels)

    # region normalize_label

    def normalize_label(self, label):
        if self.label_extra_normalization is not None:
            label = self.label_extra_normalization(label)
        label = self.scale_label(label)
        label = self.flatten_label(label)
        return label

    def scale_label(self, label):
        for li in range(0, len(label)):
            x, y = label[li]
            x /= float(self.target_width)
            y /= float(self.target_height)
            label[li] = (x, y)
        return label

    @staticmethod
    def flatten_label(label):
        fl = []
        for x, y in label:
            fl.append(x)
            fl.append(y)
        return fl

    # endregion

    # region augment_item

    def augment_item(self, image, label):
        if self.augment_data and random.random() > 0.5:
            image, label = self.flip_image_lr(image, label)

        original_height = image.shape[0]
        original_width = image.shape[1]

        if self.augment_data:
            image, label = self.random_rotate(image, label)
        image, label = self.random_scale(image, label, original_height, original_width)
        image, label = self.random_crop(image, label)

        return image, label

    # region flip_image

    def flip_image_lr(self, image, label):
        image = cv2.flip(image, 1)

        image_width = image.shape[1]
        for li in range(len(label)):
            x, y = label[li]
            label[li] = image_width - x, y

        for a, b in self.label_flip_pairs:
            t = label[a]
            label[a] = label[b]
            label[b] = t

        return image, label

    # endregion

    # region random_rotate

    def random_rotate(self, image, label):
        image_height = image.shape[0]
        image_width = image.shape[1]

        min_x, min_y, max_x, max_y = DataGenerator.get_label_bounds(label, image_height, image_width)
        if min_x < 0 or min_y < 0 or max_x >= image_width or max_y >= image_height:
            return image, label

        angle = random.uniform(-90.0, 90.0)
        if abs(angle) < .1:
            return image, label

        xc = image_width / 2
        yc = image_height / 2
        max_distance = max([DataGenerator.get_distance(min_x, min_y, xc, yc),
                            DataGenerator.get_distance(min_x, max_y, xc, yc),
                            DataGenerator.get_distance(max_x, min_y, xc, yc),
                            DataGenerator.get_distance(max_x, max_y, xc, yc)])

        max_dy = max_distance * abs(math.sin(math.radians(angle)))
        max_dx = max_distance * abs(math.cos(math.radians(angle)))
        if max_dy < 2 or max_dx < 2:  # no visible rotation
            return image, label

        increase_height = int(max([0, 0 - (min_y - max_dy), max_y + max_dy - image_height]))
        increase_width = int(max([0, 0 - (min_x - max_dx), max_x + max_dx - image_width]))
        if increase_height > 0 or increase_width > 0:
            new_height = image_height + 2 * increase_height
            new_width = image_width + 2 * increase_width
            dy = increase_height
            dx = increase_width

            new_image = np.zeros((new_height, new_width, image.shape[2]), dtype=np.uint8)
            new_image[:, :, :] = image[0, 0, :]
            new_image[dy:dy + image_height, dx:dx + image_width, :] = image

            for li in range(len(label)):
                x, y = label[li]
                x += dx
                y += dy
                label[li] = (x, y)
        else:
            new_image = image
            new_height = image_height
            new_width = image_width

        matrix = cv2.getRotationMatrix2D((new_width / 2, new_height / 2), angle, 1)
        new_image = cv2.warpAffine(new_image, matrix, (new_width, new_height), flags=cv2.INTER_LINEAR)
        new_label = cv2.transform(np.array([label]), matrix)[0]

        return new_image, new_label

    @staticmethod
    def get_distance(x0, y0, x1, y1):
        dx = x0 - x1
        dy = y0 - y1
        return math.sqrt(dx ** 2 + dy ** 2)

    # endregion

    # region random_scale

    def random_scale(self, image, label, original_height, original_width):
        image_height = image.shape[0]
        image_width = image.shape[1]

        random_coeff = random.uniform(0.8, 1.2) if self.augment_data else 1.0
        scale = max(self.target_height / original_height, self.target_width / original_width) * random_coeff
        new_height = int(image_height * scale + 0.5)
        new_width = int(image_width * scale + 0.5)

        if new_height != image_height and new_width != image_width:  # both sizes should be different (to keep aspect)
            image = cv2.resize(image, (new_width, new_height))

            for li in range(len(label)):
                x, y = label[li]
                x *= new_width / image_width
                y *= new_height / image_height
                label[li] = (x, y)

        return image, label

    # endregion

    # region random_crop

    def random_crop(self, image, label):
        image_height = image.shape[0]
        image_width = image.shape[1]
        if image_height == self.target_height and image_width == self.target_width:
            return image, label

        if self.augment_data:
            min_x, min_y, max_x, max_y = DataGenerator.get_label_bounds(label, image_height, image_width)

            min_dx, max_dx = self.get_shift_limits(min_x, max_x, image_width, self.target_width)
            min_dy, max_dy = self.get_shift_limits(min_y, max_y, image_height, self.target_height)
            dx = int(random.uniform(min_dx, max_dx) + 0.5)
            dy = int(random.uniform(min_dy, max_dy) + 0.5)
        else:
            dx = (image_width - self.target_width) // 2
            dy = (image_height - self.target_height) // 2

        for li in range(len(label)):
            x, y = label[li]
            x -= dx
            y -= dy
            label[li] = (x, y)

        sx0 = dx if dx >= 0 else 0
        sy0 = dy if dy >= 0 else 0
        sx1 = min(dx + self.target_width, image_width)
        sy1 = min(dy + self.target_height, image_height)

        tx0 = 0 if dx >= 0 else -dx
        ty0 = 0 if dy >= 0 else -dy
        tx1 = tx0 + sx1 - sx0
        ty1 = ty0 + sy1 - sy0

        result = np.zeros((self.target_height, self.target_width, 3), dtype=np.uint8)
        result[:, :, :] = image[0, 0, :]
        result[ty0:ty1, tx0:tx1, :] = image[sy0:sy1, sx0:sx1, :]

        return result, label

    @staticmethod
    def get_label_bounds(label, image_height, image_width):
        min_x = image_width
        min_y = image_height
        max_x = 0
        max_y = 0
        for x, y in label:
            if x < min_x:
                min_x = x
            if y < min_y:
                min_y = y
            if x > max_x:
                max_x = x
            if y > max_y:
                max_y = y
        return min_x, min_y, max_x, max_y

    @staticmethod
    def get_shift_limits(min_label, max_label, size, target_size):
        label_size = max_label - min_label
        if label_size < target_size:
            min_shift = max_label - target_size
            max_shift = min_label
        else:
            min_shift = min_label
            max_shift = max_label - target_size

        if min_label < 0:
            min_shift = 0
            if label_size < target_size:
                max_shift = min_shift
        if max_label > size:
            max_shift = size - target_size
            if label_size < target_size:
                min_shift = max_shift

        if max_shift < min_shift:
            t = max_shift
            max_shift = min_shift
            min_shift = t

        return min_shift, max_shift

    # endregion

    # endregion
