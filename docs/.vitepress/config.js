module.exports = {
  base: '/',
  lang: 'en-US',
  title: 'Marten',
  description: '.NET Transactional Document DB and Event Store on PostgreSQL',
  head: [
    ['link', { rel: 'apple-touch-icon', type: 'image/png', size: "180x180", href: '/apple-touch-icon.png' }],
    ['link', { rel: 'icon', type: 'image/png', size: "32x32", href: '/favicon-32x32.png' }],
    ['link', { rel: 'icon', type: 'image/png', size: "16x16", href: '/favicon-16x16.png' }],
    ['link', { rel: 'manifest', manifest: '/manifest.json' }]
  ],

  themeConfig: {
    logo: '/logo.png',
    repo: 'JasperFx/marten',
    docsDir: 'docs',
    docsBranch: 'master',
    editLinks: true,
    editLinkText: 'Suggest changes to this page',

    nav: [
      { text: 'v4.x',
        items: [
          { text: 'v3.x', link: 'https://martendb.io'}
        ]
      },
      { text: 'Guide', link: '/guide/' },
      { text: 'Document Database', link: '/guide/documents/' },
      { text: 'Event Store', link: '/guide/events/' },
      { text: 'Migration', link: '/migration-guide' },
      { text: 'Release Notes', link: 'https://github.com/JasperFx/marten/releases' },
      { text: 'Gitter | Join Chat', link: 'https://gitter.im/jasperfx/marten?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge' },
      { text: 'Twitter', link: 'https://twitter.com/intent/follow?original_referer=https%3A%2F%2Fmartendb.io%2F&ref_src=twsrc%5Etfw&region=follow_link&screen_name=marten_lib&tw_p=followbutton' },
    ],

    algolia: {
      appId: '9S7KY0SIDO',
      apiKey: '5b95a0e704fcf10d97ae621741cd907d',
      indexName: 'marten_index'
    },

    sidebar: [
      {
        text: 'Introduction',
        link: '/guide/'
      },
      {
        text:'Integration and Configuration',
        link: '/guide/configuration/',
          children: [
              {text: 'Bootstrap with HostBuilder', link: '/guide/configuration/hostbuilder'},
              {text: 'Do It Yourself IoC Integration', link: '/guide/configuration/ioc'},
              {text: 'Command Line Tooling', link: '/guide/configuration/cli'},
              {text: 'Configuring Document Storage with StoreOptions', link: '/guide/configuration/storeoptions'},
              {text: 'Json Serialization', link: '/guide/configuration/json'},
              {text: 'Retry Policies', link:'/guide/configuration/retries'},
              {text: 'Pre-Building Generated Types', link: '/guide/configuration/prebuilding'}
          ]
      },
      {
        text: 'Document Database',
        link: '/guide/documents/',
        children: [
          {text: 'Identity', link: '/guide/documents/identity'},
          {text: 'Storage', link: '/guide/documents/storage'},
          {text: 'Sessions', link: '/guide/documents/sessions'},
          {text: 'Storing', link: '/guide/documents/storing'},
          {text: 'Deleting', link: '/guide/documents/deletes'},
          {
            text: 'Querying',
            link: '/guide/documents/querying/',
            children: [
                {text: 'Load Documents by Id', link: '/guide/documents/querying/byid'},
                {text: 'Querying with Linq', link: '/guide/documents/querying/linq/', children: [
                        {text: 'Supported Linq Operators', link: '/guide/documents/querying/linq/operators'},
                        {text: 'Querying within Child Collections', link: '/guide/documents/querying/linq/child-collections'},
                        {text: 'Including Related Documents', link: '/guide/documents/querying/linq/include'},
                        {text: 'Querying to IAsyncEnumerable', link: '/guide/documents/querying/linq/async-enumerable'},
                        {text: 'Extending Marten\'s Linq Support', link: '/guide/documents/querying/linq/extending'},
                        {text: 'Searching on String Fields', link: '/guide/documents/querying/linq/strings'},
                        {text: 'Projection Operators', link: '/guide/documents/querying/linq/projections'},
                        {text: 'Paging', link: '/guide/documents/querying/linq/paging'},
                        {text: 'Mixing Raw SQL with Linq', link: '/guide/documents/querying/linq/sql'},
                        {text: 'Searching with Boolean Flags', link: '/guide/documents/querying/linq/booleans'},
                        {text: 'Searching for NULL Values', link: '/guide/documents/querying/linq/nulls'},
                    ]},
                {text: 'Querying with Postgres SQL', link: '/guide/documents/querying/sql'},
                {text: 'Querying for Raw JSON', link: '/guide/documents/querying/query-json'},
                {text: 'Compiled Queries', link: '/guide/documents/querying/compiled-queries'},
                {text: 'Batched Queries', link: '/guide/documents/querying/batched-queries'}


            ]
          },
          {
            text: 'Indexing',
            link: '/guide/documents/indexing/',
            children: [
                {text: 'Calculated Indexes', link: '/guide/documents/indexing/computed-indexes'},
                {text: 'Duplicated Fields', link: '/guide/documents/indexing/duplicated-fields'},
                {text: 'Unique Indexes', link: '/guide/documents/indexing/unique'},
                {text: 'Foreign Keys', link: '/guide/documents/indexing/foreign-keys'},
                {text: 'GIN or GiST Indexes', link: '/guide/documents/indexing/gin-gist-indexes'},
                {text: 'Metadata Indexes', link: '/guide/documents/indexing/metadata-indexes'}
            ]
          },
          {text:'Document Type Hierarchies', link: '/guide/documents/hierarchies'},
          {text:'Multi-Tenanted Documents', link: '/guide/documents/multi-tenancy'},
          {text: 'Initial Baseline Data', link: '/guide/documents/initial-data'},
          {text: 'Optimistic Concurrency', link: '/guide/documents/concurrency'},
          {text: 'Full Text Searching', link: '/guide/documents/full-text'},
          {text: 'Noda Time Support', link: '/guide/documents/noda-time'},
          {text: 'PLv8 Support', link: '/guide/documents/plv8'},
          {text: 'AspNetCore Support', link: '/guide/documents/aspnetcore'}
        ]
      },
      {
        text: 'Event Store',
        link: '/guide/events/',
        children: [
          {text: 'Introduction', link: '/guide/events/index'},
          {text: 'Quick Start', link: '/guide/events/quickstart'},
          {text: 'Configuration', link: '/guide/events/configuration'},
          {text: 'Appending Events', link: '/guide/events/appending'},
          {text: 'Querying Events', link: '/guide/events/querying'},
          {text: 'Archiving Streams', link: '/guide/events/archiving'},
          {
            text: 'Projections',
            link: '/guide/events/projections/',
            children: [
              {text: 'Aggregate projections', link: '/guide/events/projections/aggregate-projections'},
              {text: 'Live Aggregations', link: '/guide/events/projections/live-aggregates'},
              {text: 'View Projections', link: '/guide/events/projections/view-projections'},
              {text: 'Event Projections', link: '/guide/events/projections/event-projections'},
              {text: 'Custom projections', link: '/guide/events/projections/custom'},
              {text: 'Inline projections', link: '/guide/events/projections/inline'},
              {text: 'Async Daemon',link: '/guide/events/projections/async-daemon'},
              {text: 'Rebuilding projections', link: '/guide/events/projections/rebuilding'}
            ]
          },
          {
            text: 'Streaming',
            link: '/guide/events/streaming/',
            children: [
              {
                text: 'Introduction',
                link: '/guide/events/streaming/'
              },
              {
                text: 'Publishing changes',
                link: '/guide/events/streaming/source'
              },
              {
                text: 'Receiving events',
                link: '/guide/events/streaming/sink'
              },
            ]
          },
          {
            text: 'Metadata',
            link: '/guide/events/metadata'
          },
          {
            text: 'Event versioning',
            link: '/guide/events/versioning'
          },
          {
            text: 'Storage',
            link: '/guide/events/storage'
          },
          {
            text: 'Multitenancy',
            link: '/guide/events/multitenancy'
          },
          {
            text: 'Advanced',
            children: [
              {
                text: 'Aggregates, events and repositories',
                link: '/guide/scenarios/aggregates-events-repositories'
              },
              {
                text: 'Copy and transform stream',
                link: '/guide/scenarios/copy-and-transform-stream'
              },
              {
                text: 'Immutable projections as read model',
                link: '/guide/scenarios/immutable-projections-read-model'
              },
            ]
          }
        ]
      },
      {text: 'Diagnostics and Instrumentation', link: '/guide/diagnostics'},
      {
        text: 'Database Management',
        link: '/guide/schema/',
        children: [
          {text: 'How Documents are Stored', link: '/guide/schema/storage'},
          {text: 'Metadata', link: '/guide/schema/metadata'},
          {text: 'Schema Migrations', link: '/guide/schema/migrations'},
          {text: 'Exporting Schema Definition', link: '/guide/schema/exporting'},
          {text: 'Schema Feature Extensions', link: '/guide/schema/extensions'},
          {text: 'Tearing Down Document Storage', link: '/guide/schema/cleaning'},
        ]
      },
      {text: 'FAQ & Troubleshooting', link: '/guide/troubleshoot'},
      {
        text: 'Scenarios',
        link: '/guide/scenarios/',
        children: [
          {
            text: 'Aggregates, events and repositories',
            link: '/guide/scenarios/aggregates-events-repositories'
          },
          {
            text: 'Copy and transform stream',
            link: '/guide/scenarios/copy-and-transform-stream'
          },
          {
            text: 'Dynamic data',
            link: '/guide/scenarios/dynamic-data'
          },
          {
            text: 'Immutable projections as read model',
            link: '/guide/scenarios/immutable-projections-read-model'
          },
          {
            text: 'Using sequences for unique identifiers',
            link: '/guide/scenarios/using-sequence-for-unique-id'
          },
        ]
      },
      {
        text: 'Managing Postgres Instance',
        link: '/guide/admin/',
        children: [
          {
            text: 'Installing plv8 on Windows',
            link: '/guide/admin/installing-plv8-windows'
          }
        ]
      },
      {
        text: 'Postgres for SQL Server users',
        link: '/guide/postgres/',
        children: [
          {
            text: 'Naming conventions',
            link: '/guide/postgres/naming'
          },
          {
            text: 'Escaping',
            link: '/guide/postgres/escaping'
          },
          {
            text: 'Types',
            link: '/guide/postgres/types'
          },
          {
            text: 'Casting',
            link: '/guide/postgres/casting'
          },
          {
            text: 'Casing',
            link: '/guide/postgres/casing/',
            children: [
              {
                text: 'Unique values',
                link: '/guide/postgres/casing/unique-values'
              },
              {
                text: 'Case insensitive data',
                link: '/guide/postgres/casing/case-insensitive-data'
              },
              {
                text: 'Queries',
                link: '/guide/postgres/casing/queries'
              },
              {
                text: 'Using duplicate fields',
                link: '/guide/postgres/casing/using-duplicate-fields'
              },
            ]
          },
          {
            text: 'Slow queries',
            link: '/guide/postgres/slow-queries'
          },
          {
            text: 'Indexing',
            link: '/guide/postgres/indexing'
          },
          {
            text: 'Indexing JSONB',
            link: '/guide/postgres/indexing-jsonb'
          },
          {
            text: 'If statements',
            link: '/guide/postgres/if-statements'
          },
          {
            text: 'Working with dates',
            link: '/guide/postgres/dates'
          },
          {
            text: 'Backup and restore',
            link: '/guide/postgres/backup-restore/',
            children: [
              {
                text: 'Local',
                link: '/guide/postgres/backup-restore/local'
              },
              {
                text: 'Remote',
                link: '/guide/postgres/backup-restore/remote'
              }
            ]
          }
        ]
      }
    ]
  }
}
