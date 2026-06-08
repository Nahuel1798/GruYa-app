#!/bin/bash

set -e

echo "=================================="
echo "Iniciando deploy..."
echo "Proyecto: $(pwd)"
echo "=================================="

echo "[1/3] Actualizando repositorio..."
git pull

echo "[2/3] Deteniendo contenedores..."
docker-compose down -v --rmi all

echo "[3/3] Reconstruyendo e iniciando contenedores..."
docker-compose up --build -d

echo "=================================="
echo "Deploy completado correctamente"
echo "=================================="
