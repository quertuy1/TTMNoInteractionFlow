import socket
import cv2
import json
import numpy as np
import os

# Configurar OpenCV Face Detector (Haar Cascade)
# Usamos el modelo basico de caras frontales que ya viene instalado con OpenCV
cascade_path = os.path.join(cv2.data.haarcascades, 'haarcascade_frontalface_default.xml')
face_cascade = cv2.CascadeClassifier(cascade_path)

# Configurar Sockets UDP
# Python escucha en el puerto 5005 (Recibe frames de Unity)
sock_recv = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
sock_recv.bind(("127.0.0.1", 5005))
sock_recv.setblocking(False)

# Python envia resultados al puerto 5006 (Unity escucha)
sock_send = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
UNITY_ADDRESS = ("127.0.0.1", 5006)

print("⚡ Servidor IA de Face Tracking iniciado! (OpenCV)")
print("Esperando frames desde Unity por UDP (puerto 5005)...")

# Buffer grande para recibir JPEGs
MAX_DGRAM = 65507

while True:
    try:
        data, addr = sock_recv.recvfrom(MAX_DGRAM)
        
        # Decodificar el frame JPEG
        np_arr = np.frombuffer(data, np.uint8)
        img = cv2.imdecode(np_arr, cv2.IMREAD_COLOR)
        
        if img is None:
            continue
            
        # Convertir a escala de grises para el detector de OpenCV
        gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
        
        # Detectar caras
        faces = face_cascade.detectMultiScale(
            gray, 
            scaleFactor=1.1, 
            minNeighbors=5, 
            minSize=(30, 30)
        )
        
        if len(faces) > 0:
            # Tomar la primera cara detectada
            (x, y, w, h) = faces[0]
            
            # Centro de la cara en pixeles
            center_x_px = x + (w / 2.0)
            center_y_px = y + (h / 2.0)
            
            # Convertir a valores relativos de 0 a 1 para Unity
            img_h, img_w = img.shape[:2]
            center_x = center_x_px / img_w
            center_y = center_y_px / img_h
            
            # Escala estimada basada en el ancho de la caja relativo
            scale = w / img_w
            
            # Formatear datos y enviar de vuelta a Unity
            # Invertimos Y porque Unity tiene el (0,0) abajo a la izquierda y OpenCV arriba a la izquierda
            data_dict = {
                "x": center_x,
                "y": 1.0 - center_y,
                "scale": scale
            }
            
            json_data = json.dumps(data_dict).encode('utf-8')
            sock_send.sendto(json_data, UNITY_ADDRESS)
            
    except BlockingIOError:
        # No hay datos, continuar sin bloquear
        pass
    except Exception as e:
        print(f"Error: {e}")
