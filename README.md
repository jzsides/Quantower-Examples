# Quantower Examples

## Chart-Strat-Communication
This example uses a static class to share values between a Chart Indicator and a Strategy.  It was a simple POC to see if it was possible.
It would need to be expanded to handle multiple instances of the Indicators and startegies.

It should also be possible to fire events from the indicator and handle them in the stategy.

A youtube video of this running is here - https://youtu.be/yF92MSWYHsw

The cs files are from three different projects.  I put the dll for the static class in Quantower\TradingPlatform\$Version\bin

## Indicators
ChartRectButtons.cs - Draws rectangles on charts that act like buttons.  Clicking on the rectangle flips the color.

## Strategies
Breakout.cs - Rangebar breakout trader.  Included to show a method to process range bars in a strategy. 
Opens a position when a bar closes above or below the box.  A bar close is detected using the HistoricalData.NewHistoryItem event.
This signals the start of a new bar.  The code checks the close of the previous bar during the NewHistoryItem event.
Future needs - more testing. exit/reverse on opposite breakout.  Add metrics to track PNL.