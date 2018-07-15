from DataLoader import DataLoader
from keras.utils import Sequence
import cv2
import math
import random
import numpy as np
# import Visualizer


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
        random.shuffle(self.image_files)

        self.big_image = None

    def __len__(self):
        return int(np.ceil(self.images_count / float(self.batchSize)))

    def on_epoch_end(self):
        random.shuffle(self.image_files)

    def __getitem__(self, idx):
        start_index = idx * self.batchSize % self.images_count
        last_index = min(start_index + self.batchSize, self.images_count)

        batch_images = []
        batch_labels = []

        for index in range(start_index, last_index):
            image, label = self.loader.load_image_and_labels(self.image_files[index])
            image, label = self.augment_item(image, label)

            label = self.normalize_label(label)
            # Visualizer.show_image(image, label, title=self.image_files[index])
            image = image / 255.0

            batch_images.append(image)
            batch_labels.append(label)

        return np.array(batch_images), np.array(batch_labels)

    '''
    Returns images with unscaled pixels (0..255)
    '''
    def get_validation_item(self, nb):
        batch_images = []
        batch_labels = []
        for index in range(nb):
            image, label = self.loader.load_image_and_labels(self.image_files[index])

            image_height = image.shape[0]
            image_width = image.shape[1]
            dx = max(0, int((image_width - image_height / self.targetHeight * self.targetWidth) / 2 + 0.5))
            dy = max(0, int((image_height - image_width / self.targetWidth * self.targetHeight) / 2 + 0.5))
            image = image[dy:image_height-dy, dx:image_width-dx, :]
            for li in range(len(label)):
                x, y = label[li]
                x -= dx
                y -= dy
                label[li] = (x, y)

            image = cv2.resize(image, (self.targetWidth, self.targetHeight))
            label = self.normalize_label(label)

            batch_images.append(image)
            batch_labels.append(label)

        return np.array(batch_images), np.array(batch_labels)

    # region normalize_label

    def normalize_label(self, label):
        label = self.correct_label_orientation(label)
        label = self.scale_label(label)
        label = self.flatten_label(label)
        return label

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
            ta, tb = label[group[0]]  # use elements to prevent reference-copy arrays
            for i in range(len(group) - 1):
                label[group[i]] = label[group[i + 1]]
            label[group[-1]] = (ta, tb)
        return label

    def scale_label(self, label):
        for li in range(0, len(label)):
            x, y = label[li]
            x /= float(self.targetWidth)
            y /= float(self.targetHeight)
            label[li] = (x, y)
        return label

    def flatten_label(self, label):
        fl = []
        for x, y in label:
            fl.append(x)
            fl.append(y)
        return fl

    # endregion

    # region augment_item

    def augment_item(self, image, label):
        if random.random() > 0.5:
            image, label = self.flip_image_lr(image, label)

        original_height = image.shape[0]
        original_width = image.shape[1]

        image, label = self.random_rotate(image, label)
        image, label = self.random_scale(image, label, original_height, original_width)
        image, label = self.random_crop(image, label)

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

    def random_rotate(self, image, label):
        image_height = image.shape[0]
        image_width = image.shape[1]

        for lx, ly in label:
            if lx < 0 or lx > image_width or ly < 0 or ly > image_height:
                return image, label

        angle = random.uniform(-60.0, 60.0)
        if angle == 1.0:
            return image, label

        new_size = int(math.sqrt(image_height ** 2 + image_width ** 2))
        dy = int((new_size - image_height) / 2)
        dx = int((new_size - image_width) / 2)

        new_image = self.big_image
        if new_image is None or new_image.shape[0] != new_size:
            new_image = np.zeros((new_size, new_size, image.shape[2]), dtype=np.uint8)
            self.big_image = new_image
        new_image[:, :, :] = image[0, 0, :]
        new_image[dy:dy+image_height, dx:dx+image_width, :] = image

        for li in range(len(label)):
            x, y = label[li]
            x += dx
            y += dy
            label[li] = (x, y)

        matrix = cv2.getRotationMatrix2D((new_size / 2, new_size / 2), angle, 1)

        new_image = cv2.warpAffine(new_image, matrix, (new_size, new_size), flags=cv2.INTER_LINEAR)
        new_label = cv2.transform(np.array([label]), matrix)[0]

        return new_image, new_label

    def random_scale(self, image, label, original_height, original_width):
        image_height = image.shape[0]
        image_width = image.shape[1]

        scale = max(self.targetHeight / original_height, self.targetWidth / original_width) * random.uniform(0.8, 1.2)
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

    # region random_crop

    def random_crop(self, image, label):
        image_height = image.shape[0]
        image_width = image.shape[1]
        if image_height == self.targetHeight and image_width == self.targetWidth:
            return image, label

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

        min_dx, max_dx = self.get_shift_limits(min_x, max_x, image_width, self.targetWidth)
        min_dy, max_dy = self.get_shift_limits(min_y, max_y, image_height, self.targetHeight)
        dx = int(random.uniform(min_dx, max_dx) + 0.5)
        dy = int(random.uniform(min_dy, max_dy) + 0.5)

        for li in range(len(label)):
            x, y = label[li]
            x -= dx
            y -= dy
            label[li] = (x, y)

        sx0 = dx if dx >= 0 else 0
        sy0 = dy if dy >= 0 else 0
        sx1 = min(dx + self.targetWidth, image_width)
        sy1 = min(dy + self.targetHeight, image_height)

        tx0 = 0 if dx >= 0 else -dx
        ty0 = 0 if dy >= 0 else -dy
        tx1 = tx0 + sx1 - sx0
        ty1 = ty0 + sy1 - sy0

        result = np.zeros((self.targetHeight, self.targetWidth, 3), dtype=np.uint8)
        result[:, :, :] = image[0, 0, :]
        result[ty0:ty1, tx0:tx1, :] = image[sy0:sy1, sx0:sx1, :]

        return result, label

    def get_shift_limits(self, min_label, max_label, size, target_size):
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
