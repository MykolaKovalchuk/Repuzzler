from keras import applications
from keras import optimizers
from keras.models import Model
from keras.layers import Dropout, Flatten, Dense
from keras import backend as K
from keras.callbacks import ModelCheckpoint, LearningRateScheduler, TensorBoard, EarlyStopping

from Shared.DataGenerator import DataGenerator

img_width, img_height = 256, 256
train_data_dir = "/mnt/DA92812D92810F67/Rubik/Only Rubik/Processed"
train_labels_dir = "/mnt/DA92812D92810F67/Rubik/Only Rubik/Bounds"
nb_labels = 14
batch_size = 16
steps_per_epoch = 7
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
data_generator = DataGenerator(train_data_dir, train_labels_dir,
                               batch_size=batch_size,
                               target_width=img_width, target_height=img_height)

model_final.fit_generator(data_generator, steps_per_epoch=steps_per_epoch, epochs=epochs)
