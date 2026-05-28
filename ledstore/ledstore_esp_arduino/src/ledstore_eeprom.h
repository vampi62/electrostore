bool containsInvalidCharacter(String str)
{
  for (int i = 0; i < str.length(); i++)
  {
    // check if the character is equal to the invalid character (�)
    if (str.charAt(i) == '�')
    {
      return true;
    }
    // check if the character is equal to the invalid character (0xFF)
    if (str.charAt(i) == 0xFF)
    {
      return true;
    }
  }
  return false;
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