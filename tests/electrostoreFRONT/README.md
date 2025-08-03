# Cypress Tests for ElectrostoreFRONT

This directory contains Cypress tests for the ElectrostoreFRONT application.

### Adding More Translations

If you need to add more translations for your tests, you can extend the `translations` object in `cypress/support/e2e.js`. For example:

```javascript
const translations = {
  common: {
    // existing translations...
  },
  // Add a new namespace
  store: {
    VStoreTitle: "Rangement",
    // more translations...
  }
};
```

## Running Tests

To run the tests:

```bash
# start the ElectrostoreFRONT server
cd electrostoreFRONT
npm install
npm run dev

# Navigate to the tests directory
cd tests/electrostoreFRONT
npm install

# Run unit tests
npm run test:unit

# Open Cypress Test Runner
npm run cypress:open

# Run tests in headless mode
npm run cypress:run
```