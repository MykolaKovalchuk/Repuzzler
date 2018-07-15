from keras import applications
from keras import optimizers
from keras import activations
from keras import metrics
from keras.models import Model
from keras.layers import Dropout, Flatten, Dense
from keras import backend as K


def euclidean_distance_loss(y_true, y_pred):
    return K.sqrt(K.sum(K.square(y_true - y_pred), axis=-1, keepdims=True))


class ModelCreator(object):
    def __init__(self, image_width=299, image_height=299, nb_labels=14):
        self.imageWidth = image_width
        self.imageHeight = image_height
        self.nb_labels = nb_labels
        self.base_model_name = ""

    def get_model(self, loss_function=euclidean_distance_loss):
        base_model = applications.InceptionV3(weights="imagenet", include_top=False,
                                              input_shape=(self.imageWidth, self.imageHeight, 3))
        self.base_model_name = "InceptionV3"

        # Freeze base layers
        for layer in base_model.layers:
            layer.trainable = False

        # Adding custom Layers
        x = base_model.output
        x = Flatten()(x)
        x = Dense(1024, activation=activations.relu)(x)
        # x = Dropout(0.5)(x)
        x = Dense(1024, activation=activations.relu)(x)
        predictions = Dense(self.nb_labels, activation=activations.linear)(x)

        # creating the final model
        final_model = Model(inputs=base_model.input, outputs=predictions)

        # compile the model
        final_model.compile(loss=loss_function, optimizer=optimizers.SGD(lr=0.001, momentum=0.9),
                            metrics=[metrics.mean_absolute_error])

        return final_model

    @staticmethod
    def unfreeze_top(model: Model, from_level=105):
        for layer in model.layers[:from_level]:
            layer.trainable = False
        for layer in model.layers[from_level:]:
            layer.trainable = True

        # recompile the model
        model.compile(loss=euclidean_distance_loss, optimizer=optimizers.SGD(lr=0.001, momentum=0.9),
                      metrics=[metrics.mean_absolute_error])
