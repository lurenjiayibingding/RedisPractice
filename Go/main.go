package main

import (
	"redispractice/simpleredis"
)

func main() {
	simpleredis.Connect("127.0.0.1", 6379, "", 0)
	ping1, _ := simpleredis.Ping()
	print(ping1)
}
