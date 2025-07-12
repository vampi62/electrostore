FROM python:3.11.13-slim

WORKDIR /app

COPY requirements.txt .

RUN pip install --no-cache-dir -r requirements.txt

COPY . .

EXPOSE 5000

#CMD ["python", "main.py"]
CMD ["gunicorn", "--bind", "0.0.0.0:5000", "main:app"]