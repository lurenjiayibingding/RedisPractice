package simpleredis

import (
	"bufio"
	"fmt"
	"net"
	"strings"
)

var conn net.Conn
var host string
var port int
var username string
var password string
var db int

func Connect(h string, p int, un, psd string, dbNum int) (bool, string) {
	host = h
	port = p
	username = un
	password = psd
	db = dbNum
	conn, _ = net.Dial("tcp", fmt.Sprintf("%s:%d", host, port))

	if strings.TrimSpace(username) != "" || strings.TrimSpace(password) != "" {
		var command string
		var err error
		if strings.TrimSpace(username) == "" {
			command, err = TransitionCommand(fmt.Sprintf("AUTH %s", password))
		} else {
			command, err = TransitionCommand(fmt.Sprintf("AUTH %s %s", username, password))
		}
		if err != nil {
			return false, "连接到redis失败"
		}
		result, err := sendCommand(command)
		if err != nil {
			return false, "连接到redis失败"
		}
		return true, result
	}

	return true, ""
}

// 发送命令到Redis服务器
func sendCommand(command string) (string, error) {
	conn.Write([]byte(command))
	reader := bufio.NewReader(conn)
	line, err := reader.ReadString('\n')
	if err != nil {
		return "", fmt.Errorf("发送命令失败%s", err)
	}
	return line, nil
}

func Ping() (string, error) {
	return sendCommand("*1\r\n$4\r\nPING\r\n")
}

// 将输入的字符串转为redis协议
func TransitionCommand(key string) (string, error) {
	if strings.TrimSpace(key) == "" {
		return "", fmt.Errorf("输入的命令为空")
	}
	var command strings.Builder
	var commandLength int
	commandArray := strings.Split(key, "")
	for i := 0; i < len(commandArray); i++ {
		currentCommand := commandArray[i]
		if strings.TrimSpace(currentCommand) == "" {
			continue
		}
		var currentLength = len(currentCommand)
		command.WriteString(fmt.Sprintf("$%d\r\n%s\r\n", currentLength, currentCommand))
		commandLength++
	}

	result := fmt.Sprintf("*%d\r\n%s", commandLength, command.String())
	return result, nil
}

// 解析Redis返回值
func AnalysisRequest(request []byte) (string, error) {
	switch request[0] {
	case '+', '-', ':':
		return string(request[1 : len(request)-2]), nil
	default:
		return "", fmt.Errorf("解析失败")
	}
}
