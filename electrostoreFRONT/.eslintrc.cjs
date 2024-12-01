/* eslint-env node */
module.exports = {
    "root": true,
    "extends": [
        "plugin:vue/vue3-essential",
        "eslint:recommended"
    ],
    ignorePatterns: ['tailwind.config.js', 'postcss.config.js'],
    "env": {
        "vue/setup-compiler-macros": true
    },
    rules: {
      'no-constant-condition': ['error', { checkLoops: false }]
    }
}
