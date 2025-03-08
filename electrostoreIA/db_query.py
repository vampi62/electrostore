import mysql.connector

class MySQLConnection:
	def __init__(self, settings):
		self.host = settings.get('Server')
		self.database = settings.get('Database')
		self.port = settings.get('Port')
		self.user = settings.get('Uid')
		self.password = settings.get('Pwd')
		self.connection = None

	def connect(self):
		try:
			self.connection = mysql.connector.connect(
				host=self.host,
				port=self.port,
				database=self.database,
				user=self.user,
				password=self.password
			)
			if self.connection.is_connected():
				print("Connection to MySQL database was successful")
				return True
		except mysql.connector.Error as e:
			print(f"Error: {e}")
			self.connection = None
		return False

	def close(self):
		if self.connection is not None and self.connection.is_connected():
			self.connection.close()
			print("MySQL connection is closed")

	def is_connected(self):
		if self.connection is not None and self.connection.is_connected():
			return True
		return False
	

	""" public class IA
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int id_ia { get; set; }
		[MaxLength(50)]
		public string nom_ia { get; set; }
		[MaxLength(500)]
		public string description_ia { get; set; }
		public DateTime date_ia { get; set; }
		public bool trained_ia { get; set; } = false;
	} """
	def get_ia(self, id_ia):
		try:
			cursor = self.connection.cursor()
			cursor.execute(f"SELECT * FROM IA WHERE id_ia = {id_ia}")
			ia = cursor.fetchone()
			return ia
		except mysql.connector.Error as e:
			print(f"Error: {e}")
			return None

	def change_train_status(self, id_ia, trained_ia):
		try:
			cursor = self.connection.cursor()
			if trained_ia:
				cursor.execute(f"UPDATE IA SET trained_ia = 1, date_ia = NOW() WHERE id_ia = {id_ia}")
			else:
				cursor.execute(f"UPDATE IA SET trained_ia = 0 WHERE id_ia = {id_ia}")
			self.connection.commit()
			return True
		except mysql.connector.Error as e:
			print(f"Error: {e}")
			return False