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
        self.layers_to_unfreeze = 0
        self.loss_function = euclidean_distance_loss

    VGG16 = ("VGG16", applications.VGG16, 11)
    VGG19 = ("VGG19", applications.VGG19, 12)
    ResNet50 = ("ResNet50", applications.ResNet50, 154)
    InceptionV3 = ("InceptionV3", applications.InceptionV3, 249)
    Xception = ("Xception", applications.Xception, 106)

    def get_model(self, architecture, loss_function=euclidean_distance_loss):
        self.base_model_name, base_model_creator, self.layers_to_unfreeze = architecture
        self.loss_function = loss_function

        base_model = base_model_creator(weights="imagenet", include_top=False,
                                        input_shape=(self.imageWidth, self.imageHeight, 3))

        # Freeze all base layers
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
        final_model.compile(loss=self.loss_function, optimizer=optimizers.SGD(lr=0.001, momentum=0.9),
                            metrics=[metrics.mean_absolute_error])

        return final_model

    def unfreeze_top(self, model: Model, from_level=-1, new_lr=0.001):
        if from_level < 0:
            from_level = self.layers_to_unfreeze

        for layer in model.layers[:from_level]:
            layer.trainable = False
        for layer in model.layers[from_level:]:
            layer.trainable = True

        # recompile the model
        model.compile(loss=self.loss_function, optimizer=optimizers.SGD(lr=new_lr, momentum=0.9),
                      metrics=[metrics.mean_absolute_error])
