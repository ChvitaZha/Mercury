if __name__ == '__main__':
    from multiprocessing import freeze_support
    freeze_support()
    # Your main code goes here

    from ultralytics import YOLO

    # Load a model
    model = YOLO('yolov8n.pt')  # load a pretrained model (recommended for training)

    # Train the model with 2 GPUs
    results = model.train(data='E:\SHIYU\pretrain_image\data.yaml', epochs=2, imgsz=640, batch=8, workers=0)