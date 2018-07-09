from keras.utils import Sequence
from DataLoader import DataLoader
import numpy as np
import cv2


class DataGenerator(Sequence):
    def __init__(self, images_folder, labels_folder,
                 batch_size=16,
                 images_extension=".png", labels_extension=".bounds",
                 target_width=256, target_height=256):
        self.batchSize = batch_size
        self.targetWidth = target_width
        self.targetHeight = target_height

        self.loader = DataLoader(images_folder, labels_folder,
                                 images_extension, labels_extension)
        self.image_files = self.loader.get_image_file_names()
        self.images_count = len(self.image_files)

    def __len__(self):
        return int(np.ceil(self.images_count / float(self.batchSize)))

    def __getitem__(self, idx):
        start_index = idx * self.batchSize % self.images_count

        batch_images = []
        batch_labels = []

        for index in range(start_index, start_index + self.batchSize):
            real_index = index
            if real_index >= self.images_count:
                real_index = real_index - self.images_count
            image, label = self.loader.load_image_and_labels(self.image_files[real_index])

            image_height = image.shape[0]
            image_width = image.shape[1]
            for li in range(0, len(label), 2):
                label[li] = label[li] / float(image_width)
                label[li + 1] = label[li + 1] / float(image_height)

            image = cv2.resize(image, (self.targetWidth, self.targetHeight)) / 255.0

            batch_images.append(image)
            batch_labels.append(label)

        return np.array(batch_images), np.array(batch_labels)
