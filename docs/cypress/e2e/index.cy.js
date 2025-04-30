describe('Index page', () => {
  beforeEach(() =>{
    cy.visit('/')
  })

  it('loads', () => {
    cy.contains('h1', 'Introduction').should('be.visible')
  })

  it('has the Algolia search bar', () => {
    cy.window().then((win) => {
      expect(win.docsearch).to.exist
    })
  })

  it('opens the Algolia search modal when typing in the search input', () => {
    cy.get('input[placeholder="Search"]').type('intro')
    cy.get('.DocSearch-Modal').should('exist')
  })

  it('shows results in the Algolia search dropdown', () => {
    cy.get('input[placeholder="Search"]').type('intro')
    cy.get('.DocSearch-Hit').should('have.length.at.least', 1)
  })
})