from keras import applications
from keras.preprocessing.image import ImageDataGenerator
from keras import optimizers
from keras.models import Model
from keras.layers import Dropout, Flatten, Dense
from keras import backend as K
from keras.callbacks import ModelCheckpoint, LearningRateScheduler, TensorBoard, EarlyStopping
import numpy as np
import cv2

from Shared.DataLoader import Loader

img_width, img_height = 256, 256
train_data_dir = "/mnt/DA92812D92810F67/Rubik/Only Rubik/Processed"
train_labels_dir = "/mnt/DA92812D92810F67/Rubik/Only Rubik/Bounds"
nb_labels = 14
batch_size = 16
epochs = 20

model = applications.VGG19(weights="imagenet", include_top=False, input_shape=(img_width, img_height, 3))

# Freeze the layers which you don't want to train. Here I am freezing the first 5 layers.
for layer in model.layers:
    layer.trainable = False

# Adding custom Layers
index_file = model.output
index_file = Flatten()(index_file)
index_file = Dense(1024, activation="relu")(index_file)
index_file = Dropout(0.5)(index_file)
index_file = Dense(1024, activation="relu")(index_file)
predictions = Dense(14, activation="linear")(index_file)

# creating the final model
model_final = Model(inputs=model.input, outputs=predictions)


def euclidean_distance_loss(y_true, y_pred):
    return K.sqrt(K.sum(K.square(y_true - y_pred), axis=-1, keepdims=True))


# compile the model
model_final.compile(loss=euclidean_distance_loss, optimizer=optimizers.Adam(), metrics=["accuracy"])

# data
data_loader = Loader(train_data_dir, train_labels_dir)
image_files = data_loader.get_image_file_names()
nb_train_samples = len(image_files)

train_features = np.zeros(shape=(nb_train_samples, img_height, img_width, 3))
train_labels = np.zeros(shape=(nb_train_samples, nb_labels))

for index_file in range(nb_train_samples):
    image_file = image_files[index_file]
    image, labels = data_loader.load_image_and_labels(image_file)
    train_features[index_file] = cv2.resize(image, (img_width, img_height))
    train_labels[index_file] = labels

model_final.fit(train_features, train_labels, batch_size=batch_size, epochs=epochs)