import tensorflow as tf

train_dir = 'D:\git\electrostore\python\img'
test_dir = 'D:\git\electrostore\python\img'

# All images will be rescaled by 1./255
train_datagen = tf.keras.preprocessing.image.ImageDataGenerator(rescale=1./255)
test_datagen = tf.keras.preprocessing.image.ImageDataGenerator(rescale=1./255)
train_generator = train_datagen.flow_from_directory(
 train_dir,
 # All images will be resized to 150x150
 target_size=(150, 150),
 batch_size=32,
 class_mode='binary')
test_generator = test_datagen.flow_from_directory(
 test_dir,
 target_size=(150, 150),
 batch_size=32,
 class_mode='binary')

model = tf.keras.models.Sequential()
model.add(tf.keras.layers.Conv2D(32, (3, 3), activation='relu',
 input_shape=(150, 150, 3)))
model.add(tf.keras.layers.MaxPooling2D((2, 2)))
model.add(tf.keras.layers.Conv2D(64, (3, 3), activation='relu'))
model.add(tf.keras.layers.MaxPooling2D((2, 2)))
model.add(tf.keras.layers.Conv2D(128, (3, 3), activation='relu'))
model.add(tf.keras.layers.MaxPooling2D((2, 2)))
model.add(tf.keras.layers.Conv2D(128, (3, 3), activation='relu'))
model.add(tf.keras.layers.MaxPooling2D((2, 2)))
model.add(tf.keras.layers.Flatten())
model.add(tf.keras.layers.Dense(512, activation='relu'))
model.add(tf.keras.layers.Dense(1, activation='sigmoid'))

model.compile(loss='binary_crossentropy',
 optimizer=tf.keras.optimizers.experimental.RMSprop(lr=1e-4),
 metrics=['acc'])

checkpoint = tf.keras.callbacks.ModelCheckpoint(filepath='./best_model.h5', monitor="val_acc", mode="max",save_best_only=True, verbose=1)

callbacks = [checkpoint]

# Training and Validation
history = model.fit_generator(
 train_generator,
 steps_per_epoch=int(22564/32),
 epochs=50,
 validation_data=test_generator,
 validation_steps=int(2513/32),
 callbacks=callbacks)

# Save the model
model.save('D:/GIT/electrostore/python/mon_model.h5')