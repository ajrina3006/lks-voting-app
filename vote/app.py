import os
import random
import json
import logging

from flask import Flask, render_template, request, make_response, g
from redis import Redis

option_a = os.getenv('OPTION_A', "Cats")
option_b = os.getenv('OPTION_B', "Dogs")
hostname = os.environ.get('HOSTNAME', 'Unknown')

app = Flask(__name__)

logging.basicConfig(level=logging.DEBUG)

# Ganti host ke endpoint ElastiCache kamu
def get_redis():
    if not hasattr(g, 'redis'):
        g.redis = Redis(
            host="<ELASTICACHE_ENDPOINT>",  # ← ganti ini
            port=6379,
            db=0,
            socket_timeout=5
        )
    return g.redis
