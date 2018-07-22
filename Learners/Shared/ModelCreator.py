from keras import activations
from keras import applications
from keras import metrics
from keras import optimizers
from keras import backend as K
from keras.layers import Dropout, Flatten, Dense
from keras.models import load_model
from keras.models import Model
import tensorflow as tf
from tensorflow.python.framework import graph_io
from tensorflow.python.framework import graph_util
import os


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

    def get_model(self, architecture, weights_file_name=None, loss_function=euclidean_distance_loss):
        self.base_model_name, base_model_creator, self.layers_to_unfreeze = architecture
        self.loss_function = loss_function

        base_model = base_model_creator(weights="imagenet" if weights_file_name is None else None,
                                        include_top=False,
                                        input_shape=(self.imageWidth, self.imageHeight, 3))

        """ Print base model layers' indexes and names
        for li in range(len(base_model.layers)):
            print(str(li) + " : " + base_model.layers[li].name)
        # """

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
        if weights_file_name is not None:
            final_model.load_weights(weights_file_name)

        # compile the model
        final_model.compile(loss=self.loss_function,
                            optimizer=optimizers.SGD(lr=0.001, momentum=0.9),
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
        model.compile(loss=self.loss_function,
                      optimizer=optimizers.SGD(lr=new_lr, momentum=0.9),
                      metrics=[metrics.mean_absolute_error])

    def load(self, file_name):
        base_model_def = None
        for model_def in [ModelCreator.VGG16,
                          ModelCreator.VGG19,
                          ModelCreator.ResNet50,
                          ModelCreator.InceptionV3,
                          ModelCreator.Xception]:
            if model_def[0] in file_name:
                base_model_def = model_def
                break
        if base_model_def is not None:
            self.base_model_name, _, self.layers_to_unfreeze = base_model_def

        return load_model(file_name)

    @staticmethod
    def save(model: Model, file_name):
        model.save(file_name)
        print("saved keras model at: ", file_name)

    @staticmethod
    def save_tf(model: Model, folder, file_name_only, num_output=1):
        K.set_learning_phase(0)
        K.set_image_data_format("channels_last")

        pred = [None] * num_output
        pred_node_names = [None] * num_output
        for i in range(num_output):
            pred_node_names[i] = "output_" + str(i + 1)
            pred[i] = tf.identity(model.outputs[i], name=pred_node_names[i])
        print("output nodes names are: ", pred_node_names)

        sess = K.get_session()
        constant_graph = graph_util.convert_variables_to_constants(sess, sess.graph.as_graph_def(), pred_node_names)
        graph_io.write_graph(constant_graph, folder, file_name_only, as_text=False)
        print("saved frozen graph (ready for inference) at: ", os.path.join(folder, file_name_only))
