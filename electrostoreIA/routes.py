"""Flask API routes for the ElectrostoreIA application."""

import os
from flask import request, jsonify

from config import Status
from model_trainer import (
    async_train_model, get_training_status, is_training_in_progress, 
    create_demo_training_result, training_progress
)
from image_detector import detect_model


def register_routes(app, appsettings, s3_manager, mysql_session):
    """Register all Flask routes."""
    
    @app.route('/train/<int:id_model>', methods=['POST'])
    def train(id_model):
        try:
            # Check if demo mode is enabled via appsettings
            if appsettings.get('DemoMode', False):
                # In demo mode, mock the training process
                print("Demo mode enabled: Mocking training process")
                # check if the model exists in the database
                model = mysql_session.get_ia(id_model)
                if model is None:
                    return jsonify({"error": "Model not found in the database."}), 404
                # Set training as completed immediately
                training_progress[id_model] = create_demo_training_result(id_model)
                # Set trained_ia field to false in the database
                # no change to the database in demo mode
                #mysql_session.change_train_status(id_model, True)
                return jsonify({"message": f"Training for model {id_model} completed (demo mode)."}), 200

            # Normal mode - proceed with actual training
            # check if a training is already in progress
            if is_training_in_progress():
                return jsonify({"error": "Training already in progress for a model."}), 400
            
            # check if the model exists in the database
            model = mysql_session.get_ia(id_model)
            if model is None:
                return jsonify({"error": "Model not found in the database."}), 404
            
            # Initialiser le progrès dans le dictionnaire
            training_progress[id_model] = {'status': Status.IN_PROGRESS}
            # set to false the trained_ia field in the database
            mysql_session.change_train_status(id_model, False)
            # Lancer la tâche d'entraînement en arrière-plan
            async_train_model(id_model, s3_manager, mysql_session)
            return jsonify({"message": f"Training for model {id_model} started."}), 200
        except Exception as e:
            return jsonify({"error": str(e)}), 500

    @app.route('/status/<int:id_model>', methods=['GET'])
    def status(id_model):
        """Route pour récupérer l'avancement de l'entraînement."""
        status_info = get_training_status(id_model)
        if "No training in progress" in status_info.get("message", ""):
            return jsonify(status_info), 404
        return jsonify(status_info), 200

    @app.route('/detect/<int:id_model>', methods=['POST'])
    def detect(id_model):
        try:
            img_file = request.files.get('img_file')
            if img_file is None:
                return jsonify({"error": "No image file provided"}), 400
            result = detect_model(id_model, img_file, s3_manager)
            return jsonify(result), 200
        except Exception as e:
            return jsonify({"error": str(e)}), 500

    @app.route('/health', methods=['GET'])
    def health_check():
        """Route pour vérifier la santé de l'application."""
        try:
            # Test S3 connection if enabled
            s3_status = "disabled"
            if s3_manager and s3_manager.is_enabled():
                if s3_manager.test_connection():
                    s3_status = "connected"
                else:
                    s3_status = "error: connection test failed"
            
            config = {
                "status": "healthy" if str(appsettings.get('DemoMode', False)).lower() == 'false' else "demo",
                "training_in_progress": is_training_in_progress(),
                "db_connected": mysql_session is not None and mysql_session.is_connected(),
                "s3_status": s3_status
            }
            return jsonify(config), 200
        except Exception as e:
            return jsonify({"status": "unhealthy", "error": str(e)}), 500