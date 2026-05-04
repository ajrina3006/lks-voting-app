var express = require('express'),
    async = require('async'),
    { Pool } = require('pg'),
    cookieParser = require('cookie-parser'),
    app = express(),
    server = require('http').Server(app),
    io = require('socket.io')(server);

io.set('transports', ['polling']);

var port = process.env.PORT || 4000;

io.sockets.on('connection', function (socket) {
  socket.emit('connected', {});
});

// Ganti ke endpoint RDS kamu
var pool = new Pool({
  host: '<RDS_ENDPOINT>',       // ← ganti ini
  user: 'admin',
  password: 'LKSNCC2024',
  database: 'postgres',
  port: 5432
});
