const lightCodeTheme = require('./src/prism/light');
const darkCodeTheme = require('./src/prism/dark');

/** @type {import('@docusaurus/types').DocusaurusConfig} */
module.exports = {
  title: 'Arcus - Testing',
  url: 'https://testing.arcus-azure.net/',
  baseUrl: '/',
  onBrokenLinks: 'throw',
  onBrokenMarkdownLinks: 'warn',
  favicon: 'img/favicon.ico',
  organizationName: 'arcus-azure', // Usually your GitHub org/user name.
  projectName: 'Arcus - Testing', // Usually your repo name.
  themeConfig: {
    image: 'img/arcus.jpg',
    navbar: {
      title: '',
      logo: {
        alt: 'Arcus',
        src: 'img/arcus.png',
        srcDark: 'img/arcus_for_dark.png',
      },
      items: [
        {
          type: 'dropdown',
          label: 'Arcus Testing',
          position: 'left',
          items: [
            {
              label: 'Arcus Messaging',
              href: 'https://messaging.arcus-azure.net/',
            },
            {
              label: 'Arcus Observability',
              href: 'https://observability.arcus-azure.net/'
            },
            {
              label: 'Arcus Security',
              href: 'https://security.arcus-azure.net/'
            },
            {
              label: 'Arcus Scripting',
              href: 'https://scripting.arcus-azure.net/'
            }
          ]
        },
        {
          type: 'docsVersionDropdown',

          //// Optional
          position: 'right',
          // Add additional dropdown items at the beginning/end of the dropdown.
          dropdownItemsBefore: [],
          // Do not add the link active class when browsing docs.
          dropdownActiveClassDisabled: true,
          docsPluginId: 'default',
        },
        {
          type: 'search',
          position: 'right',
        },
        {
          href: 'https://github.com/arcus-azure/arcus.testing',
          label: 'GitHub',
          position: 'right',
        },
      ],
    },
    footer: {
      style: 'dark',
      links: [
        {
          title: 'Community',
          items: [
            {
              label: 'Github',
              href: 'https://github.com/arcus-azure/arcus.testing',
            },
            {
              label: 'Contribution guide',
              href: 'https://github.com/arcus-azure/arcus.testing/blob/main/CONTRIBUTING.md'
            },
            {
              label: 'Report an issue',
              href: 'https://github.com/arcus-azure/arcus.testing/issues/new/choose'
            },
            {
              label: 'Discuss an idea',
              href: 'https://github.com/arcus-azure/arcus.testing/discussions/new/choose'
            },
          ]
        },
        {
          title: 'Tech-independent',
          items: [
            {
              label: 'Core infrastructure',
              to: 'Features/core'
            },
            {
              label: 'Assertions',
              to: 'Features/assertion'
            },
            {
              label: 'Logging',
              to: 'Features/logging'
            }
          ]
        },
        {
          title: 'Azure',
          items: [
            {
              label: 'Storage account',
              to: 'Features/Azure/Storage/storage-account'
            },
            {
              label: 'Cosmos DB',
              to: 'Features/Azure/Storage/cosmos'
            },
            {
              label: 'Service Bus',
              to: 'Features/Azure/Messaging/servicebus'
            },
            {
              label: 'Data Factory',
              to: 'Features/Azure/Integration/data-factory'
            }
          ]
        },
        {
          title: 'Support',
          items: [
            {
              label: 'Getting started',
              to: 'getting-started'
            },
            {
              label: 'Migrate Testing Framework to v1',
              to: 'Guidance/migrate-from-testing-framework-to-arcus-testing-v1.0'
            },
            {
              label: 'Migrate v1 to v2',
              to: 'Guidance/migrate-from-v1-to-v2'
            }
          ]
        }
      ],
      copyright: `Copyright © ${new Date().getFullYear()}, Arcus - Testing maintained by Codit`,
    },
    prism: {
      theme: lightCodeTheme,
      darkTheme: darkCodeTheme,
      additionalLanguages: ['csharp', 'fsharp', 'diff', 'json', 'powershell'],
    },
  },
  presets: [
    [
      '@docusaurus/preset-classic',
      {
        docs: {
          sidebarPath: require.resolve('./sidebars.js'),
          routeBasePath: '/',
          path: 'preview',
          sidebarCollapsible: false,
          // Please change this to your repo.
          editUrl: 'https://github.com/arcus-azure/arcus.testing/edit/main/docs',
          includeCurrentVersion: process.env.CONTEXT !== 'production',
          admonitions: {
            keywords: ['praise'],
            extendDefaults: true,
          }
        },
        theme: {
          customCss: require.resolve('./src/css/custom.css'),
        },
      },
    ],
  ],
  stylesheets: [
    'https://fonts.googleapis.com/css2?family=Bitter:wght@700&family=Inter:wght@400;500&display=swap'
  ],
};
