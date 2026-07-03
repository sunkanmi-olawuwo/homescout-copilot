Feature: Mortgage cost estimate

  So that buyers can gauge affordability,
  HomeScout estimates the monthly mortgage repayment from their own figures.

  Scenario: Repayment mortgage monthly cost
    When I estimate a repayment mortgage for a 300000 property with a 30000 deposit at 4.5% over 25 years
    Then the estimated monthly payment is about 1500
    And the estimate is labelled not mortgage advice
