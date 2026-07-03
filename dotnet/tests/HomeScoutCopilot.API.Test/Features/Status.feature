Feature: Product status

  So that the React workspace can confirm the backend contract,
  the HomeScout API exposes its product status.

  Scenario: Status reports the API-first architecture
    When the client requests the product status
    Then the API reports it is "API-first"
    And the API names the React frontend
