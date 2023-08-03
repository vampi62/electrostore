void writeStringToEEPROM(int startAddress, const String& data) {
  int length = data.length();
  EEPROM.write(startAddress, length);
  for (int i = 0; i < length; i++) {
    EEPROM.write(startAddress + 1 + i, data[i]);
  }
  EEPROM.commit();
}

String readStringFromEEPROM(int startAddress) {
  int length = EEPROM.read(startAddress);
  String data = "";
  for (int i = 0; i < length; i++) {
    data += char(EEPROM.read(startAddress + 1 + i));
  }
  return data;
}