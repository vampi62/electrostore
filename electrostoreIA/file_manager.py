"""File management utilities for S3 synchronization and local storage."""

import os
from electrostoreIA.config import MODEL_DIR, IMAGE_DIR


def get_model_path(id_model, s3_manager=None):
    """Get the local path for a model, downloading from S3 if needed."""
    local_path = os.path.join(MODEL_DIR, f'Model{id_model}.keras')
    
    if s3_manager and s3_manager.is_enabled():
        s3_key = f'models/Model{id_model}.keras'
        # Create directory if it doesn't exist
        os.makedirs(MODEL_DIR, exist_ok=True)
        # Try to download from S3 if file doesn't exist locally
        if not os.path.exists(local_path):
            if s3_manager.download_file(s3_key, local_path):
                print(f"Downloaded model {id_model} from S3")
            else:
                raise FileNotFoundError(f"Model {id_model} not found in S3 or local storage")
    
    return local_path


def get_class_names_path(id_model, s3_manager=None):
    """Get the local path for class names file, downloading from S3 if needed."""
    local_path = os.path.join(MODEL_DIR, f'ItemList{id_model}.txt')
    
    if s3_manager and s3_manager.is_enabled():
        s3_key = f'models/ItemList{id_model}.txt'
        # Create directory if it doesn't exist
        os.makedirs(MODEL_DIR, exist_ok=True)
        # Try to download from S3 if file doesn't exist locally
        if not os.path.exists(local_path):
            if s3_manager.download_file(s3_key, local_path):
                print(f"Downloaded class names {id_model} from S3")
            else:
                raise FileNotFoundError(f"Class names file for model {id_model} not found in S3 or local storage")
    
    return local_path


def _download_images_from_s3(local_dir, image_objects, s3_manager):
    """Download images from S3 to local directory."""
    s3_relative_paths = set()
    
    for s3_key in image_objects:
        # Extract relative path from S3 key (remove 'images/' prefix)
        relative_path = s3_key[7:] if s3_key.startswith('images/') else s3_key
        s3_relative_paths.add(relative_path)
        
        # get item id folder and create it if needed
        item_id = relative_path.split('/')[0]
        item_dir = os.path.join(local_dir, item_id)
        os.makedirs(item_dir, exist_ok=True)

        local_path = os.path.join(local_dir, relative_path)
        
        # Only download if file doesn't exist locally
        if not os.path.exists(local_path):
            if s3_manager.download_file(s3_key, local_path):
                print(f"Downloaded image: {relative_path}")
    
    return s3_relative_paths


def _get_local_relative_paths(local_dir):
    """Get all relative paths of local files."""
    local_relative_paths = set()
    for root, dirs, files in os.walk(local_dir):
        for file in files:
            local_path = os.path.join(root, file)
            relative_path = os.path.relpath(local_path, local_dir).replace('\\', '/')
            local_relative_paths.add(relative_path)
    return local_relative_paths


def _delete_obsolete_files(local_dir, files_to_delete):
    """Delete local files that don't exist in S3."""
    for relative_path in files_to_delete:
        local_path = os.path.join(local_dir, relative_path.replace('/', os.sep))
        try:
            os.remove(local_path)
            print(f"Deleted local file not in S3: {relative_path}")
        except OSError as e:
            print(f"Error deleting {relative_path}: {str(e)}")


def _clean_empty_directories(local_dir):
    """Remove empty directories."""
    for root, dirs, files in os.walk(local_dir, topdown=False):
        for dir_name in dirs:
            dir_path = os.path.join(root, dir_name)
            try:
                if not os.listdir(dir_path):  # Check if directory is empty
                    os.rmdir(dir_path)
                    print(f"Deleted empty directory: {os.path.relpath(dir_path, local_dir)}")
            except OSError as e:
                print(f"Error deleting directory {dir_path}: {str(e)}")


def get_images_directory(s3_manager=None):
    """Get the images directory path, synchronizing with S3 if enabled."""
    local_dir = IMAGE_DIR
    
    if s3_manager and s3_manager.is_enabled():
        # Create directory if it doesn't exist
        os.makedirs(local_dir, exist_ok=True)
        
        # List all images in S3 and download them
        image_objects = s3_manager.list_objects('images/')
        s3_relative_paths = _download_images_from_s3(local_dir, image_objects, s3_manager)
        
        # Get local files and find obsolete ones
        local_relative_paths = _get_local_relative_paths(local_dir)
        files_to_delete = local_relative_paths - s3_relative_paths
        
        # Clean up obsolete files and empty directories
        _delete_obsolete_files(local_dir, files_to_delete)
        _clean_empty_directories(local_dir)
    
    return local_dir