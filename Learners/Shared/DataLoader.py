import os
import cv2


class Loader(object):
    def __init__(self, images_folder, labels_folder,
                 images_extension=".png", labels_extension=".bounds",
                 image_width=1920.0, image_height=1080.0, dx=0.0, dy=0.0):
        self.imagesFolder = images_folder
        self.labelsFolder = labels_folder
        self.imagesExtension = images_extension
        self.labelsExtension = labels_extension
        self.imageWidth = image_width
        self.imageHeight = image_height
        self.dx = dx
        self.dy = dy

    def get_image_file_names(self):
        file_names = []

        for image_file in os.listdir(self.imagesFolder):
            if image_file.endswith(self.imagesExtension):
                label_file = os.path.splitext(image_file)[0] + self.labelsExtension
                if os.path.isfile(os.path.join(self.labelsFolder, label_file)):
                    file_names.append(image_file)

        return file_names

    def load_labels(self, full_labels_file_name):
        labels = []

        with open(full_labels_file_name) as f:
            label_lines = f.read().split("\n")
        for line in label_lines:
            if line.strip():
                parts = line.split(",")
                x = (int(parts[0]) - self.dx) / self.imageWidth
                y = (int(parts[1]) - self.dy) / self.imageHeight
                labels.append(x)
                labels.append(y)

        return labels

    def load_image_and_labels(self, image_file_name):
        full_image_file_name = os.path.join(self.imagesFolder, image_file_name)
        labels_file_name = os.path.splitext(image_file_name)[0] + self.labelsExtension
        full_labels_file_name = os.path.join(self.labelsFolder, labels_file_name)

        image = cv2.imread(full_image_file_name)
        labels = self.load_labels(full_labels_file_name)

        return image, labels