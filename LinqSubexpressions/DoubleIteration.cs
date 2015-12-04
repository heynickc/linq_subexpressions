using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqSubexpressions.DAL;
using Serilog;
using Serilog.Events;
using SerilogMetrics;
using Xunit;
using Xunit.Abstractions;

namespace LinqSubexpressions {
    public class DoubleIteration : IDisposable {
        private readonly ITestOutputHelper _output;
        private readonly IDisposable _logCapture;
        private readonly ICounterMeasure _calcLinePriceCounter;
        private readonly ICounterMeasure _calcLineCostCounter;
        private readonly ICounterMeasure _calcMarginCounter;

        public DoubleIteration(ITestOutputHelper output) {
            _output = output;
            _logCapture = LoggingHelper.Capture(_output);
            _calcLinePriceCounter = Log.Logger.CountOperation(
                "CalculateLinePrice Counter",
                "operation(s)",
                false);
            _calcLineCostCounter = Log.Logger.CountOperation(
                "CalculateLineCost Counter",
                "operation(s)",
                false);
            _calcMarginCounter = Log.Logger.CountOperation(
                "CalculateMargin Counter",
                "operation(s)",
                false);
        }
        [Fact]
        public void Multliple_method_calls_linetotal() {
            using (var db = new FakeMyDbContext()) {

                var sods = new List<SalesOrderDetail>();
                for (int i = 0; i < 10; i++) {
                    sods.Add(new SalesOrderDetail() {
                        SalesOrderId = 71774,
                        ProductId = 905,
                        OrderQty = 4,
                        UnitPrice = 218.454m
                    });
                }
                db.SalesOrderDetails.AddRange(sods);

                using (Log.Logger.BeginTimedOperation("Calculating quick total", "Test")) {
                    _calcLinePriceCounter.Reset();
                    var results = db.SalesOrderDetails
                        .Where(sod => sod.SalesOrderId == 71774)
                        .ToList()
                        .Select(sod => new {
                            sod.OrderQty,
                            sod.UnitPrice,
                            LineTotal = CalculateLinePrice(
                                sod.OrderQty,
                                sod.UnitPrice,
                                _calcLinePriceCounter)
                        }).ToList();

                    _calcLinePriceCounter.Write();
                }
            }
        }
        [Fact]
        public void Multliple_method_calls_margin() {
            using (var db = new FakeMyDbContext()) {

                var sods = new List<SalesOrderDetail>();
                for (int i = 0; i < 10; i++) {
                    sods.Add(new SalesOrderDetail() {
                        SalesOrderId = 71774,
                        ProductId = 905,
                        OrderQty = 4,
                        UnitPrice = 218.454m
                    });
                }
                db.SalesOrderDetails.AddRange(sods);

                var products = new Product() {
                    ProductId = 905,
                    StandardCost = 199.3757m
                };
                db.Products.Add(products);

                using (Log.Logger.BeginTimedOperation("Calculating margin slowly", "Test")) {
                    _calcLinePriceCounter.Reset();
                    _calcLineCostCounter.Reset();
                    _calcMarginCounter.Reset();
                    var results = db.SalesOrderDetails
                        .Where(sod => sod.SalesOrderId == 71774)
                        .Join(db.Products,
                            sod => sod.ProductId,
                            product => product.ProductId,
                            (sod, product) => new { sod, product })
                        .ToList()
                        .Select(li => new {
                            li.sod.OrderQty,
                            li.sod.UnitPrice,
                            li.product.StandardCost,
                            LineTotal = CalculateLinePrice(
                                li.sod.OrderQty,
                                li.sod.UnitPrice,
                                _calcLinePriceCounter),
                            LineCost = CalculateLineCost(
                                li.sod.OrderQty,
                                li.product.StandardCost,
                                _calcLineCostCounter),
                            Margin = CalculateMargin(
                                CalculateLineCost(
                                    li.sod.OrderQty,
                                    li.product.StandardCost,
                                    _calcLineCostCounter),
                                CalculateLinePrice(
                                    li.sod.OrderQty,
                                    li.sod.UnitPrice,
                                    _calcLinePriceCounter),
                                _calcMarginCounter)
                        }).ToList();

                    _calcLinePriceCounter.Write();
                    _calcLineCostCounter.Write();
                    _calcMarginCounter.Write();
                }
            }
        }
        [Fact]
        public void Iterate_results_twice_margin() {
            using (var db = new FakeMyDbContext()) {

                var sods = new List<SalesOrderDetail>();
                for (int i = 0; i < 10; i++) {
                    sods.Add(new SalesOrderDetail() {
                        SalesOrderId = 71774,
                        ProductId = 905,
                        OrderQty = 4,
                        UnitPrice = 218.454m
                    });
                }
                db.SalesOrderDetails.AddRange(sods);

                var products = new Product() {
                    ProductId = 905,
                    StandardCost = 199.3757m
                };
                db.Products.Add(products);

                using (Log.Logger.BeginTimedOperation("Calculating margin with second iteration", "Test")) {
                    _calcLinePriceCounter.Reset();
                    _calcLineCostCounter.Reset();
                    _calcMarginCounter.Reset();
                    var results = db.SalesOrderDetails
                        .Where(sod => sod.SalesOrderId == 71774)
                        .Join(db.Products,
                            sod => sod.ProductId,
                            product => product.ProductId,
                            (sod, product) => new { sod, product })
                        .ToList()
                        .Select(li => new {
                            li.sod.OrderQty,
                            li.sod.UnitPrice,
                            li.product.StandardCost,
                            LineTotal = CalculateLinePrice(
                                li.sod.OrderQty,
                                li.sod.UnitPrice,
                                _calcLinePriceCounter),
                            LineCost = CalculateLineCost(
                                li.sod.OrderQty,
                                li.product.StandardCost,
                                _calcLineCostCounter)
                        }).ToList();

                    var resultsAgain = results
                        .Select(li => new {
                            li.OrderQty,
                            li.UnitPrice,
                            li.StandardCost,
                            li.LineTotal,
                            li.LineCost,
                            Margin = CalculateMargin(
                                li.LineCost,
                                li.LineTotal,
                                _calcMarginCounter)
                        }).ToList();

                    _calcLinePriceCounter.Write();
                    _calcLineCostCounter.Write();
                    _calcMarginCounter.Write();
                }
            }
        }

        [Fact]
        public void Multiline_lambda_statement() {
            using (var db = new FakeMyDbContext()) {

                var sods = new List<SalesOrderDetail>();
                for (int i = 0; i < 10; i++) {
                    sods.Add(new SalesOrderDetail() {
                        SalesOrderId = 71774,
                        ProductId = 905,
                        OrderQty = 4,
                        UnitPrice = 218.454m
                    });
                }
                db.SalesOrderDetails.AddRange(sods);

                var products = new Product() {
                    ProductId = 905,
                    StandardCost = 199.3757m
                };
                db.Products.Add(products);

                using (Log.Logger.BeginTimedOperation("Calculating margin with second iteration", "Test")) {
                    _calcLinePriceCounter.Reset();
                    _calcLineCostCounter.Reset();
                    _calcMarginCounter.Reset();
                    var results = db.SalesOrderDetails
                        .Where(sod => sod.SalesOrderId == 71774)
                        .Join(db.Products,
                            sod => sod.ProductId,
                            product => product.ProductId,
                            (sod, product) => new { sod, product })
                        .ToList()
                        .Select(li => {
                            var lineTotal = CalculateLinePrice(
                                li.sod.OrderQty,
                                li.sod.UnitPrice,
                                _calcLinePriceCounter);
                            var lineCost = CalculateLineCost(
                                li.sod.OrderQty,
                                li.product.StandardCost,
                                _calcLineCostCounter);
                            return new {
                                li.sod.OrderQty,
                                li.sod.UnitPrice,
                                li.product.StandardCost,
                                LineTotal = lineTotal,
                                LineCost = lineCost,
                                Margin = CalculateMargin(
                                    lineCost,
                                    lineTotal,
                                    _calcMarginCounter)
                            };
                        }).ToList();

                    _calcLinePriceCounter.Write();
                    _calcLineCostCounter.Write();
                    _calcMarginCounter.Write();
                }
            }
        }

        [Fact]
        public void Linq_subexpression_method_syntax() {
            using (var db = new FakeMyDbContext()) {

                var sods = new List<SalesOrderDetail>();
                for (int i = 0; i < 10; i++) {
                    sods.Add(new SalesOrderDetail() {
                        SalesOrderId = 71774,
                        ProductId = 905,
                        OrderQty = 4,
                        UnitPrice = 218.454m
                    });
                }
                db.SalesOrderDetails.AddRange(sods);

                var products = new Product() {
                    ProductId = 905,
                    StandardCost = 199.3757m
                };
                db.Products.Add(products);

                using (Log.Logger.BeginTimedOperation("Calculating margin with subexpression", "Test")) {
                    _calcLinePriceCounter.Reset();
                    _calcLineCostCounter.Reset();
                    _calcMarginCounter.Reset();
                    var results = db.SalesOrderDetails
                        .Where(sod => sod.SalesOrderId == 71774)
                        .Join(db.Products,
                            sod => sod.ProductId,
                            product => product.ProductId,
                            (sod, product) => new { sod, product })
                        .ToList()
                        .Select(li =>
                            new {
                                lineTotal = CalculateLinePrice(
                                    li.sod.OrderQty,
                                    li.sod.UnitPrice,
                                    _calcLinePriceCounter),
                                lineCost = CalculateLineCost(
                                    li.sod.OrderQty,
                                    li.product.StandardCost,
                                    _calcLineCostCounter),
                                li
                            })
                        .Select(lineItem =>
                            new {
                                lineItem.li.sod.OrderQty,
                                lineItem.li.sod.UnitPrice,
                                lineItem.li.product.StandardCost,
                                LineTotal = lineItem.lineTotal,
                                LineCost = lineItem.lineCost,
                                Margin = CalculateMargin(
                                    lineItem.lineCost,
                                    lineItem.lineTotal,
                                    _calcMarginCounter)
                            }).ToList();

                    _calcLinePriceCounter.Write();
                    _calcLineCostCounter.Write();
                    _calcMarginCounter.Write();
                }
            }
        }

        [Fact]
        public void Linq_subexpression_query_syntax() {
            using (var db = new FakeMyDbContext()) {

                var sods = new List<SalesOrderDetail>();
                for (int i = 0; i < 10; i++) {
                    sods.Add(new SalesOrderDetail() {
                        SalesOrderId = 71774,
                        ProductId = 905,
                        OrderQty = 4,
                        UnitPrice = 218.454m
                    });
                }
                db.SalesOrderDetails.AddRange(sods);

                var products = new Product() {
                    ProductId = 905,
                    StandardCost = 199.3757m
                };
                db.Products.Add(products);

                using (Log.Logger.BeginTimedOperation("Calculating margin with second iteration", "Test")) {
                    _calcLinePriceCounter.Reset();
                    _calcLineCostCounter.Reset();
                    _calcMarginCounter.Reset();

                    var results =
                        (from li in
                             (from sod in db.SalesOrderDetails
                              where sod.SalesOrderId == 71774
                              join product in db.Products on sod.ProductId equals product.ProductId
                              select new { sod, product }).ToList()
                         let lineTotal = CalculateLinePrice(
                             li.sod.OrderQty,
                             li.sod.UnitPrice,
                             _calcLinePriceCounter)
                         let lineCost = CalculateLineCost(
                             li.sod.OrderQty,
                             li.product.StandardCost,
                             _calcLineCostCounter)
                         select new {
                             li.sod.OrderQty,
                             li.sod.UnitPrice,
                             li.product.StandardCost,
                             LineTotal = lineTotal,
                             LineCost = lineCost,
                             Margin = CalculateMargin(
                                 lineCost,
                                 lineTotal,
                                 _calcMarginCounter)
                         }).ToList();

                    //_output.WriteLine(results.ToJson());

                    _calcLinePriceCounter.Write();
                    _calcLineCostCounter.Write();
                    _calcMarginCounter.Write();
                }
            }
        }

        [Fact]
        public void Linq_subexpression_parallel_linq() {
            using (var db = new FakeMyDbContext()) {

                var sods = new List<SalesOrderDetail>();
                for (int i = 0; i < 10; i++) {
                    sods.Add(new SalesOrderDetail() {
                        SalesOrderId = 71774,
                        ProductId = 905,
                        OrderQty = 4,
                        UnitPrice = 218.454m
                    });
                }
                db.SalesOrderDetails.AddRange(sods);

                var products = new Product() {
                    ProductId = 905,
                    StandardCost = 199.3757m
                };
                db.Products.Add(products);

                using (Log.Logger.BeginTimedOperation("Calculating margin with second iteration", "Test")) {
                    _calcLinePriceCounter.Reset();
                    _calcLineCostCounter.Reset();
                    _calcMarginCounter.Reset();
                    var results = db.SalesOrderDetails
                        .Where(sod => sod.SalesOrderId == 71774)
                        .Join(db.Products,
                            sod => sod.ProductId,
                            product => product.ProductId,
                            (sod, product) => new { sod, product })
                        .ToList()
                        .AsParallel()
                        .Select(li =>
                            new {
                                lineTotal = CalculateLinePrice(
                                    li.sod.OrderQty,
                                    li.sod.UnitPrice,
                                    _calcLinePriceCounter),
                                lineCost = CalculateLineCost(
                                    li.sod.OrderQty,
                                    li.product.StandardCost,
                                    _calcLineCostCounter),
                                li
                            })
                        .Select(lineItem => {
                            return new {
                                lineItem.li.sod.OrderQty,
                                lineItem.li.sod.UnitPrice,
                                lineItem.li.product.StandardCost,
                                LineTotal = lineItem.lineTotal,
                                LineCost = lineItem.lineCost,
                                Margin = CalculateMargin(lineItem.lineCost, lineItem.lineTotal, _calcMarginCounter)
                            };
                        }).ToList();

                    _calcLinePriceCounter.Write();
                    _calcLineCostCounter.Write();
                    _calcMarginCounter.Write();
                }
            }
        }

        public decimal CalculateLinePrice(short qty, decimal unitPrice, ICounterMeasure counter) {
            Thread.Sleep(500);
            counter.Increment();
            return qty * unitPrice;
        }

        public decimal CalculateLineCost(short qty, decimal unitCost, ICounterMeasure counter) {
            Thread.Sleep(500);
            counter.Increment();
            return qty * unitCost;
        }
        public decimal CalculateMargin(decimal lineCost, decimal linePrice, ICounterMeasure counter) {
            counter.Increment();
            var margin = (linePrice - lineCost) / linePrice;
            return margin;
        }
        public void Dispose() {
            _logCapture.Dispose();
        }
    }
}
