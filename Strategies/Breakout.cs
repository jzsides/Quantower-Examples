// Copyright QUANTOWER LLC. © 2017-2021. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using TradingPlatform.BusinessLayer;

namespace OHTBreakout
{
    /// <summary>
    /// A blank strategy. Add your code, compile it and run via Strategy Runner panel in the assigned trading terminal.
    /// Information about API you can find here: http://api.quantower.com
    /// Code samples: https://github.com/Quantower/Examples
    /// </summary>
    public class OHTBreakout : Strategy, ICurrentAccount, ICurrentSymbol
    {
        [InputParameter("Symbol")]
        public Symbol CurrentSymbol { get; set; }

        [InputParameter("Account")]
        public Account CurrentAccount { get; set; }

        [InputParameter("Buy Level")]
        public double BuyLevel { get; set; }

        [InputParameter("Sell Level")]
        public double SellLevel { get; set; }

        [InputParameter("Quantity")]
        public int Quantity { get; set; }

        [InputParameter("Profit Target Ticks")]
        public int ProfitTargetTicks { get; set; }

        [InputParameter("Stop Loss Ticks")]
        public int StopLossTicks { get; set; }

        [InputParameter("Range Bar Ticks")]
        public int RangeBarTicks { get; set; }

        [InputParameter("Range Bar Duration Hours")]
        public int RangeBarDurationHours { get; set; }

        private HistoricalData hdm;
        private bool waitOpenPosition;
        private bool waitClosePositions;
        private int longPositionsCount;
        private int shortPositionsCount;
        private int openedPositions;
        private string orderTypeId;


        /// <summary>
        /// Strategy's constructor. Contains general information: name, description etc. 
        /// </summary>
        public OHTBreakout()
            : base()
        {
            // Defines strategy's name and description.
            this.Name = "Breakout";
            this.Description = "My strategy's annotation";
        }

        /// <summary>
        /// This function will be called after creating a strategy
        /// </summary>
        protected override void OnCreated()
        {
            // Add your code here
        }

        /// <summary>
        /// This function will be called after running a strategy
        /// </summary>
        protected override void OnRun()
        {
            // Restore account object from acive connection
            if (this.CurrentAccount != null && this.CurrentAccount.State == BusinessObjectState.Fake)
                this.CurrentAccount = Core.Instance.GetAccount(this.CurrentAccount.CreateInfo());

            // Restore symbol object from acive connection
            if (this.CurrentSymbol != null && this.CurrentSymbol.State == BusinessObjectState.Fake)
                this.CurrentSymbol = Core.Instance.GetSymbol(this.CurrentSymbol.CreateInfo());

            if (this.CurrentSymbol == null || this.CurrentAccount == null || this.CurrentSymbol.ConnectionId != this.CurrentAccount.ConnectionId)
            {
                this.Log("Incorrect input parameters... Symbol or Account are not specified or they have different connectionID.", StrategyLoggingLevel.Error);
                return;
            }

            this.orderTypeId = Core.OrderTypes.FirstOrDefault(x => x.ConnectionId == this.CurrentSymbol.ConnectionId && x.Behavior == OrderTypeBehavior.Market).Id;

            if (string.IsNullOrEmpty(this.orderTypeId))
            {
                this.Log("Connection of selected symbol has not support market orders", StrategyLoggingLevel.Error);
                return;
            }

            openedPositions = 0;

            Core.PositionAdded += this.Core_PositionAdded;
            Core.PositionRemoved += this.Core_PositionRemoved;
            Core.OrdersHistoryAdded += this.Core_OrdersHistoryAdded;
            Core.TradeAdded += Core_TradeAdded;

            
            this.hdm = this.CurrentSymbol.GetHistory(new HistoryAggregationRangeBars(this.RangeBarTicks), HistoryType.Last, DateTime.Now.AddHours(-1 * this.RangeBarDurationHours));

            //current bar updated by new quote
            //this.hdm.HistoryItemUpdated += Hdm_HistoryItemUpdated;

            //New bar created
            this.hdm.NewHistoryItem += Hdm_NewHistoryItem;
        }

        

        private void Hdm_NewHistoryItem(object sender, HistoryEventArgs e)
        {

            if (this.waitOpenPosition)
                return;

            if (this.waitClosePositions)
                return;

            if (hdm.Count < 2)
                return;

            HistoryItemBar barItem = hdm[1] as HistoryItemBar;
            HistoryItemBar prevBarItem = hdm[2] as HistoryItemBar;

            if (barItem == null || prevBarItem == null)
            {
                this.Log("Null Bar", StrategyLoggingLevel.Error);
                return;
            }

            
            var positions = Core.Instance.Positions.Where(x => x.Symbol == this.CurrentSymbol && x.Account == this.CurrentAccount).ToArray();

            if (positions.Any())
            {
                //Check for exits
            }
            else
            {
                this.Log($"barOpen: {barItem.Open} barClose: {barItem.Close} prevBarOpen: {prevBarItem.Open} prevBarClose: {prevBarItem.Close} ticksize: {this.CurrentSymbol.TickSize} stopLoss: {this.CurrentSymbol.TickSize * this.StopLossTicks} profitTarget: {this.CurrentSymbol.TickSize * this.ProfitTargetTicks}", StrategyLoggingLevel.Trading);
                if ( (barItem.Close > this.BuyLevel)  && (barItem.Open <= this.BuyLevel || prevBarItem.Close <= this.BuyLevel))
                {
                    //Go Long
                    this.waitOpenPosition = true;
                    this.Log("Start open buy position");
                    var result = Core.Instance.PlaceOrder(new PlaceOrderRequestParameters()
                    {
                        Account = this.CurrentAccount,
                        Symbol = this.CurrentSymbol,

                        OrderTypeId = this.orderTypeId,
                        Quantity = this.Quantity,
                        Side = Side.Buy,
                        StopLoss = SlTpHolder.CreateSL(this.StopLossTicks, PriceMeasurement.Offset),
                        TakeProfit = SlTpHolder.CreateSL(this.ProfitTargetTicks, PriceMeasurement.Offset),
                    });

                    if (result.Status == TradingOperationResultStatus.Failure)
                        this.ProcessTradingRefuse();
                    else
                        this.Log($"Position open: {result.Status}", StrategyLoggingLevel.Trading);
                    
                }
                else if( (barItem.Close < this.SellLevel) && (barItem.Close >= this.SellLevel || prevBarItem.Close >= this.SellLevel))
                {
                    //Go Short
                    this.waitOpenPosition = true;
                    this.Log("Start open sell position");
                    var result = Core.Instance.PlaceOrder(new PlaceOrderRequestParameters()
                    {
                        Account = this.CurrentAccount,
                        Symbol = this.CurrentSymbol,

                        OrderTypeId = this.orderTypeId,
                        Quantity = this.Quantity,
                        Side = Side.Sell,
                        StopLoss = SlTpHolder.CreateSL(this.StopLossTicks, PriceMeasurement.Offset),
                        TakeProfit = SlTpHolder.CreateSL(this.ProfitTargetTicks, PriceMeasurement.Offset),
                    });
                    

                    if (result.Status == TradingOperationResultStatus.Failure)
                        this.ProcessTradingRefuse();
                    else
                        this.Log($"Position open: {result.Status}", StrategyLoggingLevel.Trading);
                }

            }
    

        }

        private void ProcessTradingRefuse()
        {
            this.Log("Strategy received refuse for trading action. It should be stopped", StrategyLoggingLevel.Error);
            this.Stop();
        }

        private void Core_PositionAdded(Position obj)
        {
            var positions = Core.Instance.Positions.Where(x => x.Symbol == this.CurrentSymbol && x.Account == this.CurrentAccount).ToArray();
            this.longPositionsCount = positions.Count(x => x.Side == Side.Buy);
            this.shortPositionsCount = positions.Count(x => x.Side == Side.Sell);

            double currentPositionsQty = positions.Sum(x => x.Side == Side.Buy ? x.Quantity : -x.Quantity);

            if (Math.Abs(currentPositionsQty) == this.Quantity)
            {
                this.waitOpenPosition = false;
                openedPositions += this.Quantity;
            }
        }

        private void Core_PositionRemoved(Position obj)
        {
            var positions = Core.Instance.Positions.Where(x => x.Symbol == this.CurrentSymbol && x.Account == this.CurrentAccount).ToArray();
            this.longPositionsCount = positions.Count(x => x.Side == Side.Buy);
            this.shortPositionsCount = positions.Count(x => x.Side == Side.Sell);

            if (!positions.Any())
                this.waitClosePositions = false;
        }

        private void Core_OrdersHistoryAdded(OrderHistory obj)
        {
            if (obj.Symbol == this.CurrentSymbol)
                return;

            if (obj.Account == this.CurrentAccount)
                return;

            if (obj.Status == OrderStatus.Refused)
                this.ProcessTradingRefuse();

            
        }

        private void Core_TradeAdded(Trade obj)
        {
            this.Log($"Trade Added - Symbol: {obj.Symbol} Order Id: {obj.OrderId} Unique Id: {obj.UniqueId} Gross PNL: {obj.GrossPnl} Net PNL: {obj.NetPnl} State: {obj.State}", StrategyLoggingLevel.Trading);
            //this.Log($"Trade Added - {obj.ToString()}", StrategyLoggingLevel.Trading);
            
        }

        /// <summary>
        /// This function will be called after stopping a strategy
        /// </summary>
        protected override void OnStop()
        {
            // Add your code here
            Core.PositionAdded -= this.Core_PositionAdded;
            Core.PositionRemoved -= this.Core_PositionRemoved;
            Core.TradeAdded -= Core_TradeAdded;

            Core.OrdersHistoryAdded -= this.Core_OrdersHistoryAdded;

            if (this.hdm != null)
            {
                this.hdm.NewHistoryItem -= Hdm_NewHistoryItem;
                this.hdm.Dispose();
            }

            base.OnStop();
        }

        /// <summary>
        /// This function will be called after removing a strategy
        /// </summary>
        protected override void OnRemove()
        {
            // Add your code here
        }

        /// <summary>
        /// Use this method to provide run time information about your strategy. You will see it in StrategyRunner panel in trading terminal
        /// </summary>
        protected override List<StrategyMetric> OnGetMetrics()
        {
            List<StrategyMetric> result = base.OnGetMetrics();

            // An example of adding custom strategy metrics:
            // result.Add("Opened buy orders", "2");
            // result.Add("Opened sell orders", "7");
            result.Add("Buy Level", BuyLevel);
            result.Add("Sell Level", SellLevel);
            result.Add("Opened Positions", openedPositions);

            return result;
        }
    }
}