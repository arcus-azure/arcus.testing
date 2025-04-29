const buildConfig = require('./docusaurus.config');

module.exports = {
  ...buildConfig,
  themeConfig: {
    ...buildConfig.themeConfig,
    algolia: {
      appId: 'HTBVPW3ZI8',
      apiKey: 'dad662ed8cb6d18cfdfe8767fb742516',
      indexName: 'testing-arcus-azure',
      // Set `contextualSearch` to `true` when having multiple versions!!!
      contextualSearch: true,
      searchParameters: {
        facetFilters: ['tags:testing'],
      },
    },
  },
};
