# Electrostore Tests

This directory contains unit tests for the Electrostore project. The tests are organized by module:

## electrostoreAPI

C# unit tests for the ASP.NET Core API. These tests use xUnit and Moq for testing.

### Running the tests

```bash
cd electrostoreAPI
dotnet test ../tests/electrostoreAPI
```

## electrostoreFRONT

Vue.js unit tests for the frontend. These tests use Vitest and Vue Test Utils for testing.

### Running the tests

```bash
cd tests/electrostoreFRONT
npm run test:unit
npm run cypress:run # before start the serveur with `npm run dev` in the electrostoreFRONT directory
```

## electrostoreIA

Python unit tests for the Flask API. These tests use pytest and pytest-flask for testing.

### Running the tests

```bash
cd electrostoreIA
python -m venv venv
# LINUX
source venv/bin/activate
pip install -r requirements.txt
pip install -r ../tests/electrostoreIA/requirements.txt
test=true python -m pytest ../tests/electrostoreIA
# WINDOWS
.venv\Scripts\activate.bat
pip install -r requirements.txt
pip install -r ../tests/electrostoreIA/requirements.txt
set test=true && python -m pytest ../tests/electrostoreIA
```