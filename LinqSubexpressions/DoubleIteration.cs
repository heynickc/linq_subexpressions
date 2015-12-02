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
    public class DoubleIteration {
        private readonly ITestOutputHelper _output;

        public DoubleIteration(ITestOutputHelper output) {
            _output = output;
        }
        [Fact]
        public void Iterate_results_twice_lineTotal() {
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

                var logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.XunitTestOutput(_output)
                    .CreateLogger();

                using (logger.BeginTimedOperation("Iterate twice timer")) {
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

                var logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.XunitTestOutput(_output)
                    .CreateLogger();

                using (logger.BeginTimedOperation("Iterate twice timer")) {
                    var results = db.SalesOrderDetails
                        .Where(sod => sod.SalesOrderId == 71774)
                        .Join(db.Products,
                            sod => sod.ProductId,
                            product => product.ProductId,
                            (sod, product) => new {sod, product})
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
                    
                    _output.WriteLine(results.ToJson());
                }
            }
        }

        public decimal CalculateLinePrice(short qty, decimal unitPrice) {
            //Thread.Sleep(1000);
            return qty * unitPrice;
        }

        public decimal CalculateLineCost(short qty, decimal unitCost) {
            //Thread.Sleep(10000);
            return qty * unitCost;
        }
        public decimal CalculateMargin(decimal lineCost, decimal linePrice) {
            var margin = (linePrice - lineCost)/linePrice;
            return margin;
        }
    }
}
