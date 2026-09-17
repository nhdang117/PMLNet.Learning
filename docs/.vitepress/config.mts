import { defineConfig } from 'vitepress'

export default defineConfig({
  title: 'PML .NET for E3D',
  description: 'A practical guide to .NET customisation for AVEVA Everything3D',
  lang: 'en-US',
  base: '/PMLNet.Learning/',
  cleanUrls: true,
  lastUpdated: true,
  head: [
    ['meta', { name: 'theme-color', content: '#3a6ea5' }]
  ],
  themeConfig: {
    nav: [
      { text: 'Guide', link: '/guide/introduction' },
      { text: 'Reference', link: '/reference/api-cheatsheet' },
      { text: 'Claude Skill', link: '/skill' }
    ],
    sidebar: {
      '/guide/': [
        {
          text: 'Foundations',
          items: [
            { text: '1. Introduction', link: '/guide/introduction' },
            { text: '2. .NET Customisation Overview', link: '/guide/dotnet-customisation-overview' },
            { text: '3. Project Setup', link: '/guide/project-setup' },
            { text: '4. API and DLL Map', link: '/guide/api-and-dlls' }
          ]
        },
        {
          text: 'Core Techniques',
          items: [
            { text: '5. PMLNetCallable Class', link: '/guide/pmlnetcallable-class' },
            { text: '6. Database Interface', link: '/guide/database-interface' },
            { text: '7. Collections and Filters', link: '/guide/collections-and-filters' }
          ]
        },
        {
          text: 'Building Applications',
          items: [
            { text: '8. PMLNetCallable User Control', link: '/guide/pmlnetcallable-user-control' },
            { text: '9. .NET Addin', link: '/guide/dotnet-addin' },
            { text: '10. Pseudo UDA', link: '/guide/pseudo-uda' },
            { text: '11. Standalone Interface', link: '/guide/standalone-interface' }
          ]
        }
      ],
      '/reference/': [
        {
          text: 'Reference',
          items: [
            { text: 'API Cheatsheet', link: '/reference/api-cheatsheet' },
            { text: 'DLL Map', link: '/reference/dll-map' },
            { text: 'Type Mapping', link: '/reference/type-mapping' },
            { text: 'Troubleshooting', link: '/reference/troubleshooting' },
            { text: 'Glossary', link: '/reference/glossary' }
          ]
        }
      ]
    },
    socialLinks: [
      { icon: 'github', link: 'https://github.com/nhdang117/PMLNet.Learning' }
    ],
    search: { provider: 'local' },
    outline: { level: [2, 3] },
    editLink: {
      pattern: 'https://github.com/nhdang117/PMLNet.Learning/edit/main/docs/:path',
      text: 'Edit this page on GitHub'
    },
    footer: {
      message: 'Unofficial community guide. Not affiliated with or endorsed by AVEVA.',
      copyright: 'Released under the MIT License.'
    }
  }
})
