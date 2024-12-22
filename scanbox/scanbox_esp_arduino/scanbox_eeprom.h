bool containsInvalidCharacter(String str)
{
  // Parcourt chaque caractère de la chaîne
  for (int i = 0; i < str.length(); i++)
  {
    // Vérifie si le caractère est égal au caractère invalide (�)
    if (str.charAt(i) == '�')
    {
      return true; // Caractère invalide trouvé
    }
    // Vérifie si le caractère est égal au caractère invalide (0xFF)
    if (str.charAt(i) == 0xFF)
    {
      return true; // Caractère invalide trouvé
    }
  }
  return false; // Aucun caractère invalide trouvé
}

void writeStringToEEPROM(int startAddress, const String &data)
{
  int length = data.length();
  EEPROM.write(startAddress, length);
  for (int i = 0; i < length; i++)
  {
    EEPROM.write(startAddress + 1 + i, data[i]);
  }
  EEPROM.commit();
}

String readStringFromEEPROM(int startAddress)
{
  int length = EEPROM.read(startAddress);
  String data = "";
  for (int i = 0; i < length; i++)
  {
    data += char(EEPROM.read(startAddress + 1 + i));
  }
  if (containsInvalidCharacter(data))
  {
    return "";
  }
  return data;
}