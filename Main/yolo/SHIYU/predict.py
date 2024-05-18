from ultralytics import YOLO

model = YOLO('best.pt')

results = model.predict(source = 'test.jpg', show=True, conf=0.3, save=True)