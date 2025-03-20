package simpleredis

import (
	"bufio"
	"fmt"
	"net"
)

var conn net.Conn
var host string
var port int
var password string
var db int

func Connect(h string, p int, psd string, dbNum int) {
	host = h
	port = p
	password = psd
	db = dbNum
	conn, _ = net.Dial("tcp", fmt.Sprintf("%s:%d", host, port))
}

func sendCommand(command string) (string, error) {
	conn.Write([]byte(command))
	reader := bufio.NewReader(conn)
	line, err := reader.ReadString('\n')
	if err != nil {
		panic(err)
		// print(err)
	}
	return line, nil
}

func Ping() (string, error) {
	return sendCommand("*1\r\n$4\r\nPING\r\n")
}
