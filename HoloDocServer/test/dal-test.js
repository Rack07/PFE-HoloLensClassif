var assert = require('assert');

var globals = require ('../utils/globals.js');
globals.dbUri = 'mongodb://localhost/test';

var dal = require ('../database/dal.js');

describe('Testing database access layer - ', function() {
  this.timeout(5000);
  let d1, d2, d3, d4, l1, l2;

  beforeEach(function(done) {
    dal.createDocument('one', 'article', '', 'arnaud', Date.Now, 'one.png', [],
      function(doc1) {
        d1 = doc1;
        dal.createDocument('two', 'picture', '', 'arnaud', Date.Now, 'two.png', [],
          function(doc2) {
            d2 = doc2;
            dal.createDocument('three', 'article', '', 'arnaud', Date.Now, 'three.png', [],
              function(doc3) {
                d3 = doc3;
                dal.createDocument('four', 'album', '', 'arnaud', Date.Now, 'four.png', [],
                  function(doc4) {
                    d4 = doc4;
                  dal.createLink(doc1._id, doc2._id,
                    function (link1){
                      l1 = link1;
                      dal.createLink(doc2._id, doc3._id,
                        function (link2){
                          l2 = link2;
                          done();
                        }, function (err){}
                      );

                    }, function (err){}
                  );
                }, function (err){}
              );
              }, function (err){}
            );
          }, function (err){}
        );
      }, function (err){}
    );
  });

  afterEach(function(done) {
    dal.dropEverything(function () {
      d1 = undefined;
      d2 = undefined;
      d3 = undefined;
      d4 = undefined;
      l1 = undefined;
      l2 = undefined;
      done();
    });
  });

  describe('Testing the initialisation', function () {
    it('Adding documents and links', function (done) {
      assert (d1 != undefined);
      assert (d2 != undefined);
      assert (d3 != undefined);
      assert (d4 != undefined);
      assert (l1 != undefined);
      assert (l2 != undefined);
      done();
    });

    it('Get Document function - naive', function (done) {
      dal.getDocuments({name: 'one'}, function (err, docs) {
        assert (docs.length == 1);
        let doc = docs[0];
        assert(String(doc._id) == String(d1._id));
        done();
      });

    });

    it('Get Document function - search something that does not exist', function (done) {
      dal.getDocuments({name: 'five'}, function (err, docs) {
        assert (docs.length == 0);
        done();
      });

    });

    it('Get Document function - multiple', function (done) {
      dal.getDocuments({label: 'article'}, function (err, docs) {
        assert (docs.length == 2);
        let doc1 = docs[0];
        let doc2 = docs[1];
        assert(String(doc1._id) == String(d1._id));
        assert(String(doc2._id) == String(d3._id));
        done();
      });
    });

    it('Get Document function - get all', function (done) {
      dal.getDocuments({}, function (err, docs) {
        assert (docs.length == 4);
        let doc1 = docs[0];
        let doc2 = docs[1];
        let doc3 = docs[2];
        let doc4 = docs[3];
        assert(String(doc1._id) == String(d1._id));
        assert(String(doc2._id) == String(d2._id));
        assert(String(doc4._id) == String(d4._id));
        done();
      });
    });

    it('Get Document Count function', function (done) {
      dal.getDocumentCount().then(function(count) {
        assert(count == 4);
        done();
      })
    });
  });

  describe('Testing the document update function', function () {
    it('Updating an existing document', function (done) {
      dal.getDocuments({name: 'one'}, function (err, docs) {
        dal.updateDocument(docs[0]._id, {label: 'picture'}, function (doc) {
          dal.getDocuments({_id: doc._id}, function(err, docs){
            assert(docs.length == 1);

            assert(String(docs[0]._id) == String(d1._id));
            assert(String(docs[0].label) != String(d1.label));
            assert(String(docs[0].label) == 'picture');
            done();
          });
        });
      });
    });
  });

  describe('Testing the areConnected function', function () {
    it('Between two direct neighboors', function (done) {
      dal.areConnected(String(d1._id), String(d2._id), function (connected) {
        assert(connected);
        done();
      });
    });

    it('Between indirect neighboors', function (done) {
      dal.areConnected(String(d1._id), String(d3._id), function (connected) {
        assert(connected);
        done();
      });
    });

    it('Between two non connected document', function (done) {
      dal.areConnected(String(d1._id), String(d4._id), function (connected) {
        assert(!connected);
        done();
      });
    });
  });

  describe('Testing the link suppression', function () {
    it('Existing link', function (done) {
      dal.deleteLink(d1._id, d2._id, function () {
        done();
      }, function () {});
    });

    it('Non Existing link', function (done) {
      dal.deleteLink(d1._id, d3._id, function () {}, function () {
        done();
      });
    });
  });
});