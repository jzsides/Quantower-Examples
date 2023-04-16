# Quantower Examples

## Chart-Strat-Communication
This example uses a static class to share values between a Chart Indicator and a Strategy.  It was a simple POC to see if it was possible.
It would need to be expanded to handle multiple instances of the Indicators and startegies.

It should also be possible to fire events from the indicator and handle them in the stategy.

A youtube video of this running is here - https://youtu.be/yF92MSWYHsw

The cs files are from three different projects.  I put the dll for the static class in Quantower\TradingPlatform\$Version\bin

## Indicators
ChartRectButtons.cs - Draws rectangles on charts that act like buttons.  Clicking on the rectangle flips the color.