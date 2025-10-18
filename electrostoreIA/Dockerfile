FROM python:3.11.13-slim

WORKDIR /app

COPY requirements.txt .

RUN pip install --no-cache-dir -r requirements.txt

COPY ["app_init.py", "config.py", "db_query.py", "file_manager.py", "image_detector.py", "main.py", "model_trainer.py", "routes.py", "S3Manager.py", "./"]

COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh && chown -R 3216:3216 /app

USER 3216:3216

EXPOSE 5000

ENTRYPOINT ["/entrypoint.sh"]

CMD ["gunicorn", "--bind", "0.0.0.0:5000", "main:app"]