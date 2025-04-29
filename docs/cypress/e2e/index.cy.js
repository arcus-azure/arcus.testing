describe('Index page', () => {
  it('loads', () => {
    cy.visit('/')
    cy.contains('h1', 'Introduction').should('be.visible')
  })
})