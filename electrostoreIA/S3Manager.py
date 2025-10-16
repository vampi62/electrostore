import os
from urllib.parse import urlparse
from minio import Minio
from minio.error import S3Error

class S3Manager:
	"""Class to manage all S3/MinIO communications."""
	def __init__(self, config=None):
		self.client = None
		self.bucket_name = None
		self.enabled = False
		if config and config.get("Enable", "false").lower() == "true":
			try:
				# Parse the service URL to get endpoint and secure flag
				parsed_url = urlparse(config["ServiceUrl"])
				endpoint = f"{parsed_url.hostname}:{parsed_url.port}" if parsed_url.port else parsed_url.hostname
				secure = parsed_url.scheme == "https"
				self.client = Minio(
					endpoint=endpoint,
					access_key=config["AccessKey"],
					secret_key=config["SecretKey"],
					secure=secure
				)
				self.bucket_name = config["BucketName"]
				self.enabled = True
				print(f"S3 connection initialized with bucket: {self.bucket_name}")
			except Exception as e:
				print(f"Warning: Could not initialize S3 connection: {str(e)}")
				self.client = None
				self.bucket_name = None
				self.enabled = False
	
	def is_enabled(self):
		"""Check if S3 is enabled and properly configured."""
		return self.enabled and self.client is not None and self.bucket_name is not None
	
	def download_file(self, s3_key, local_path):
		"""Download a file from S3 to local path."""
		if not self.is_enabled():
			raise ValueError("S3 is not enabled or configured")
		try:
			# Create directory if it doesn't exist
			os.makedirs(os.path.dirname(local_path), exist_ok=True)
			self.client.fget_object(self.bucket_name, s3_key, local_path)
			return True
		except S3Error as e:
			print(f"Error downloading {s3_key} from S3: {str(e)}")
			return False
	
	def upload_file(self, local_path, s3_key):
		"""Upload a file from local path to S3."""
		if not self.is_enabled():
			raise ValueError("S3 is not enabled or configured")
		try:
			self.client.fput_object(self.bucket_name, s3_key, local_path)
			return True
		except S3Error as e:
			print(f"Error uploading {local_path} to S3: {str(e)}")
			return False
	
	def list_objects(self, prefix):
		"""List objects in S3 with given prefix."""
		if not self.is_enabled():
			return []
		try:
			objects = self.client.list_objects(self.bucket_name, prefix=prefix, recursive=True)
			return [obj.object_name for obj in objects]
		except S3Error as e:
			print(f"Error listing S3 objects with prefix {prefix}: {str(e)}")
			return []
	
	def test_connection(self):
		"""Test the S3 connection."""
		if not self.is_enabled():
			return False
		try:
			# Try to list objects to test connection
			list(self.client.list_objects(self.bucket_name, max_keys=1))
			return True
		except Exception as e:
			print(f"S3 connection test failed: {str(e)}")
			return False
