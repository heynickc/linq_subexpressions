using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqSubexpressions.DAL;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace LinqSubexpressions {
    public class DoubleIteration : IDisposable {
        private readonly ITestOutputHelper _output;
        private readonly IDisposable _logCapture;
        public DoubleIteration(ITestOutputHelper output) {
            _output = output;
            _logCapture = LoggingHelper.Capture(_output);
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

                using (Log.Logger.BeginTimedOperation("Calculating quick total")) {
                    var results = db.SalesOrderDetails
                        .Where(sod => sod.SalesOrderId == 71774)
                        .ToList()
                        .Select(sod => new {
                            sod.OrderQty,
                            sod.UnitPrice,
                            LineTotal = CalculateLinePrice(sod.OrderQty, sod.UnitPrice)
                        }).ToList();
                    //_output.WriteLine(results.ToJson());
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

                using (Log.Logger.BeginTimedOperation("Calculating margin slowly")) {
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
                            LineTotal = CalculateLinePrice(li.sod.OrderQty, li.sod.UnitPrice),
                            LineCost = CalculateLineCost(li.sod.OrderQty, li.product.StandardCost),
                            Margin = CalculateMargin(
                                CalculateLineCost(li.sod.OrderQty, li.product.StandardCost),
                                CalculateLinePrice(li.sod.OrderQty, li.sod.UnitPrice))
                        }).ToList();

                    //_output.WriteLine(results.ToJson());
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

                using (Log.Logger.BeginTimedOperation("Calculating margin with second iteration")) {
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
                            LineTotal = CalculateLinePrice(li.sod.OrderQty, li.sod.UnitPrice),
                            LineCost = CalculateLineCost(li.sod.OrderQty, li.product.StandardCost)
                        }).ToList();

                    var resultsAgain = results
                        .Select(li => new {
                            li.OrderQty,
                            li.UnitPrice,
                            li.StandardCost,
                            li.LineTotal,
                            li.LineCost,
                            Margin = CalculateMargin(li.LineCost, li.LineTotal)
                        }).ToList();

                    //_output.WriteLine(resultsAgain.ToJson());
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

                using (Log.Logger.BeginTimedOperation("Calculating margin with second iteration")) {
                    var results = db.SalesOrderDetails
                        .Where(sod => sod.SalesOrderId == 71774)
                        .Join(db.Products,
                            sod => sod.ProductId,
                            product => product.ProductId,
                            (sod, product) => new { sod, product })
                        .ToList()
                        .Select(li => {
                            var lineTotal = CalculateLinePrice(li.sod.OrderQty, li.sod.UnitPrice);
                            var lineCost = CalculateLineCost(li.sod.OrderQty, li.product.StandardCost);
                            return new {
                                li.sod.OrderQty,
                                li.sod.UnitPrice,
                                li.product.StandardCost,
                                LineTotal = lineTotal,
                                LineCost = lineCost,
                                Margin = CalculateMargin(lineCost, lineTotal)
                            };
                        }).ToList();

                    //_output.WriteLine(resultsAgain.ToJson());
                }
            }
        }

        [Fact]
        public void Linq_subexpression() {
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

                using (Log.Logger.BeginTimedOperation("Calculating margin with second iteration")) {
                    var results = db.SalesOrderDetails
                        .Where(sod => sod.SalesOrderId == 71774)
                        .Join(db.Products,
                            sod => sod.ProductId,
                            product => product.ProductId,
                            (sod, product) => new { sod, product })
                        .ToList()
                        .Select(li =>
                            new {
                                lineTotal = CalculateLinePrice(li.sod.OrderQty, li.sod.UnitPrice),
                                lineCost = CalculateLineCost(li.sod.OrderQty, li.product.StandardCost),
                                li
                            })
                        .Select(lineItem => {
                            return new {
                                lineItem.li.sod.OrderQty,
                                lineItem.li.sod.UnitPrice,
                                lineItem.li.product.StandardCost,
                                LineTotal = lineItem.lineTotal,
                                LineCost = lineItem.lineCost,
                                Margin = CalculateMargin(lineItem.lineCost, lineItem.lineTotal)
                            };
                        }).ToList();

                    //_output.WriteLine(resultsAgain.ToJson());
                }
            }
        }

        public decimal CalculateLinePrice(short qty, decimal unitPrice) {
            Thread.Sleep(100);
            return qty * unitPrice;
        }

        public decimal CalculateLineCost(short qty, decimal unitCost) {
            Thread.Sleep(200);
            return qty * unitCost;
        }
        public decimal CalculateMargin(decimal lineCost, decimal linePrice) {
            var margin = (linePrice - lineCost) / linePrice;
            return margin;
        }

        public void Dispose() {
            _logCapture.Dispose();
        }
    }
}
