const buildConfig = require('./docusaurus.config');

module.exports = {
  ...buildConfig,
  themeConfig: {
    ...buildConfig.themeConfig,
    algolia: {
      appId: process.env.ALGOLIA_APP_ID,
      apiKey: process.env.ALGOLIA_API_KEY,
      indexName: 'testing-arcus-azure',
      // Set `contextualSearch` to `true` when having multiple versions!!!
      contextualSearch: false,
      searchParameters: {
        facetFilters: ['tags:testing'],
      },
    },
  },
};
