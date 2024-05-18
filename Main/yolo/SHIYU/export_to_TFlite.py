from ultralytics import YOLO

# Load the YOLOv8 model
model = YOLO('best.pt')

# Export the model to TFLite format
model.export(format='onnx', opset=10, imgsz=(1000,700)) # creates 'yolov8n_float32.tflite'
