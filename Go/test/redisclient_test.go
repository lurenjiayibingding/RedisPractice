package test

import (
	"testing"

	"redispractice/simpleredis"
)

func TestPing(t *testing.T) {
	simpleredis.Connect("127.0.0.1", 6379, "", 0)
	ping1, err := simpleredis.Ping()
	if err != nil {
		t.Error("Error in Ping")
	}
	print(ping1)
}

func TestHello(t *testing.T) {
	hello := simpleredis.Hello()
	if hello != "Hello, world." {
		t.Error("Expected 'Hello, world.' but got ", hello)
	}
}
