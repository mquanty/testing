import random
import math

class Complex:
    def __init__(self, re, im):
        self.re = re
        self.im = im

def cmplx(re, im):
    return Complex(re, im)

def cadd(a, b):
    return Complex(a.re + b.re, a.im + b.im)

def csub(a, b):
    return Complex(a.re - b.re, a.im - b.im)

def cmul(a, b):
    return Complex(a.re * b.re - a.im * b.im, a.re * b.im + a.im * b.re)

def cdiv(a, b):
    denom = b.re * b.re + b.im * b.im
    return Complex((a.re * b.re + a.im * b.im) / denom, (a.im * b.re - a.re * b.im) / denom)

def cdivreal(a, b):
    return Complex(a.re / b, a.im / b)

def conjg(a):
    return Complex(a.re, -a.im)

def caccum(a, b):
    a.re += b.re
    a.im += b.im

def cmulreal(a, b):
    return Complex(a.re * b, a.im * b)

def cabs(a):
    return math.sqrt(a.re * a.re + a.im * a.im)

def max(a, b):
    return a if a > b else b

def min(a, b):
    return a if a < b else b

class TcMatrix:
    def __init__(self, size):
        self.size = size
        self.elements = [[cmplx(0.0, 0.0) for _ in range(size)] for _ in range(size)]

    def set_element(self, i, j, value):
        self.elements[i-1][j-1] = value

    def get_element(self, i, j):
        return self.elements[i-1][j-1]

    def mult_by_const(self, const):
        for i in range(self.size):
            for j in range(self.size):
                self.elements[i][j] = cmulreal(self.elements[i][j], const)

    def mv_mult(self, result, vector):
        for i in range(self.size):
            result[i] = cmplx(0.0, 0.0)
            for j in range(self.size):
                result[i] = cadd(result[i], cmul(self.elements[i][j], vector[j]))

    def invert(self):
        error = 0
        lt = [0] * self.size
        t1 = 0.0
        k = 1
        for m in range(self.size):
            for ll in range(self.size):
                if lt[ll] != 1:
                    rmy = abs(self.elements[ll][ll]) - abs(t1)
                    if rmy > 0.0:
                        t1 = self.elements[ll][ll]
                        k = ll
            rmy = abs(t1)
            if rmy == 0.0:
                error = 2
                break
            t1 = 0.0
            lt[k] = 1
            for i in range(self.size):
                if i != k:
                    for j in range(self.size):
                        if j != k:
                            self.elements[i][j] -= self.elements[i][k] * self.elements[k][j] / self.elements[k][k]
            self.elements[k][k] = -1.0 / self.elements[k][k]
            for i in range(self.size):
                if i != k:
                    self.elements[i][k] *= self.elements[k][k]
                    self.elements[k][i] *= self.elements[k][k]
        for j in range(self.size):
            for k in range(self.size):
                self.elements[j][k] = -self.elements[j][k]
        return error

def set_amatrix(amatrix):
    a = cmplx(-0.5, 0.866025403)
    aa = cmplx(-0.5, -0.866025403)
    for i in range(1, 4):
        amatrix.set_element(1, i, cmplx(1.0, 0.0))
    amatrix.set_element(2, 2, aa)
    amatrix.set_element(3, 3, aa)
    amatrix.set_element(2, 3, a)

def bessel_i0(a):
    max_term = 1000
    epsilon_sqr = 1.0E-20
    result = cmplx(1.0, 0.0)
    z_sqr_25 = cmul(cmul(a, a), 0.25)
    term = z_sqr_25
    caccum(result, z_sqr_25)
    i = 1
    while i <= max_term:
        term = cmul(z_sqr_25, term)
        i += 1
        term = cdivreal(term, i * i)
        caccum(result, term)
        size_sqr = term.re * term.re + term.im * term.im
        if i > max_term or size_sqr < epsilon_sqr:
            break
    return result

def bessel_i1(x):
    max_term = 1000
    epsilon_sqr = 1.0E-20
    term = cdivreal(x, 2)
    result = term
    inc_term = term
    i = 4
    while i <= max_term:
        new_term = cdivreal(x, i)
        term = cmul(term, cmul(inc_term, new_term))
        caccum(result, term)
        inc_term = new_term
        i += 2
        size_sqr = term.re * term.re + term.im * term.im
        if i > max_term or size_sqr < epsilon_sqr:
            break
    return result

def rcd_sum(data, count):
    result = 0.0
    for i in range(count):
        result += data[i]
    return result

def gauss(mean, std_dev):
    a = 0.0
    for _ in range(12):
        a += random.random()
    return (a - 6.0) * std_dev + mean

def quasi_log_normal(mean):
    return math.exp(gauss(0.0, 1.0)) * mean

def pct_nema_unbalance(vph):
    vmag = [cabs(v) for v in vph]
    vavg = sum(vmag) / 3.0
    max_diff = max(abs(v - vavg) for v in vmag)
    if vavg != 0.0:
        return max_diff / vavg * 100.0
    else:
        return 0.0

def dbl_inc(x, y):
    x += y

def terminal_power_in(v, i, nphases):
    result = cmplx(0.0, 0.0)
    for j in range(nphases):
        caccum(result, cmul(v[j], conjg(i[j])))
    return result

def calc_k_powers(kwkvar, v, i, n):
    for j in range(n):
        kwkvar[j] = cmulreal(cmul(v[j], conjg(i[j])), 0.001)

def phase2sym_comp(vph, v012, amatrix):
    for i in range(amatrix.size):
        v012[i] = cmplx(0.0, 0.0)
        for j in range(amatrix.size):
            v012[i] = cadd(v012[i], cmul(amatrix.get_element(i+1, j+1), vph[j]))

def sym_comp2phase(vph, v012, amatrix):
    for i in range(amatrix.size):
        vph[i] = cmplx(0.0, 0.0)
        for j in range(amatrix.size):
            vph[i] = cadd(vph[i], cmul(amatrix.get_element(i+1, j+1), v012[j]))

def phase2ab0(vph, vab0, clarke_f):
    for i in range(clarke_f.size):
        vab0[i] = cmplx(0.0, 0.0)
        for j in range(clarke_f.size):
            vab0[i] = cadd(vab0[i], cmul(clarke_f.get_element(i+1, j+1), vph[j]))

def ab02phase(vph, vab0, clarke_r):
    for i in range(clarke_r.size):
        vph[i] = cmplx(0.0, 0.0)
        for j in range(clarke_r.size):
            vph[i] = cadd(vph[i], cmul(clarke_r.get_element(i+1, j+1), vab0[j]))

def set_clarke_matrices(clarke_f, clarke_r):
    sin2pi3 = math.sin(2.0 * math.pi / 3.0)
    clarke_f.set_element(1, 1, cmplx(1.0, 0.0))
    clarke_f.set_element(1, 2, cmplx(-0.5, 0.0))
    clarke_f.set_element(1, 3, cmplx(-0.5, 0.0))
    clarke_f.set_element(2, 2, cmplx(sin2pi3, 0.0))
    clarke_f.set_element(2, 3, cmplx(-sin2pi3, 0.0))
    clarke_f.set_element(3, 1, cmplx(0.5, 0.0))
    clarke_f.set_element(3, 2, cmplx(0.5, 0.0))
    clarke_f.set_element(3, 3, cmplx(0.5, 0.0))
    clarke_f.mult_by_const(2.0 / 3.0)
    clarke_r.set_element(1, 1, cmplx(1.0, 0.0))
    clarke_r.set_element(2, 1, cmplx(-0.5, 0.0))
    clarke_r.set_element(3, 1, cmplx(-0.5, 0.0))
    clarke_r.set_element(2, 2, cmplx(sin2pi3, 0.0))
    clarke_r.set_element(3, 2, cmplx(-sin2pi3, 0.0))
    clarke_r.set_element(1, 3, cmplx(1.0, 0.0))
    clarke_r.set_element(2, 3, cmplx(1.0, 0.0))
    clarke_r.set_element(3, 3, cmplx(1.0, 0.0))

def curve_mean_and_std_dev(py, px, n):
    s = 0.0
    for i in range(n - 1):
        s += 0.5 * (py[i] + py[i+1]) * (px[i+1] - px[i])
    mean = s / (px[n-1] - px[0])
    s = 0.0
    for i in range(n - 1):
        dy1 = py[i] - mean
        dy2 = py[i+1] - mean
        s += 0.5 * (dy1 * dy1 + dy2 * dy2) * (px[i+1] - px[i])
    std_dev = math.sqrt(s / (px[n-1] - px[0]))
    return mean, std_dev

def rcd_mean_and_std_dev(pdata, ndata):
    if ndata == 1:
        mean = pdata[0]
        std_dev = pdata[0]
        return mean, std_dev
    mean = rcd_sum(pdata, ndata) / ndata
    s = 0.0
    for i in range(ndata):
        s += (mean - pdata[i]) ** 2
    std_dev = math.sqrt(s / (ndata - 1))
    return mean, std_dev

def get_xr(a):
    if a.re != 0.0:
        result = a.im / a.re
        if abs(result) > 9999.0:
            result = 9999.0
    else:
        result = 9999.0
    return result

def parallel_z(z1, z2):
    denom = cadd(z1, z2)
    if denom.re != 0.0 or denom.im != 0.0:
        result = cdiv(cmul(z1, z2), denom)
    else:
        result = cmplx(0.0, 0.0)
    return result

def terminal_power_in(v, i, nphases):
    result = cmplx(0.0, 0.0)
    for j in range(nphases):
        caccum(result, cmul(v[j], conjg(i[j])))
    return result

def pct_nema_unbalance(vph):
    vmag = [cabs(v) for v in vph]
    vavg = sum(vmag) / 3.0
    max_diff = max(abs(v - vavg) for v in vmag)
    if vavg != 0.0:
        return max_diff / vavg * 100.0
    else:
        return 0.0

def dbl_inc(x, y):
    x += y

def set_clarke_matrices():
    clarke_f = TcMatrix(3)
    clarke_r = TcMatrix(3)
    set_clarke_matrices(clarke_f, clarke_r)
    return clarke_f, clarke_r

def etk_invert(a, norder):
    error = 0
    lt = [0] * norder
    t1 = 0.0
    k = 1
    for m in range(norder):
        for ll in range(norder):
            if lt[ll] != 1:
                rmy = abs(a[ll][ll]) - abs(t1)
                if rmy > 0.0:
                    t1 = a[ll][ll]
                    k = ll
        rmy = abs(t1)
        if rmy == 0.0:
            error = 2
            break
        t1 = 0.0
        lt[k] = 1
        for i in range(norder):
            if i != k:
                for j in range(norder):
                    if j != k:
                        a[i][j] -= a[i][k] * a[k][j] / a[k][k]
        a[k][k] = -1.0 / a[k][k]
        for i in range(norder):
            if i != k:
                a[i][k] *= a[k][k]
                a[k][i] *= a[k][k]
    for j in range(norder):
        for k in range(norder):
            a[j][k] = -a[j][k]
    return error

def calc_k_powers(kwkvar, v, i, n):
    for j in range(n):
        kwkvar[j] = cmulreal(cmul(v[j], conjg(i[j])), 0.001)

def phase2sym_comp(vph, v012, amatrix):
    for i in range(amatrix.size):
        v012[i] = cmplx(0.0, 0.0)
        for j in range(amatrix.size):
            v012[i] = cadd(v012[i], cmul(amatrix.get_element(i+1, j+1), vph[j]))

def sym_comp2phase(vph, v012, amatrix):
    for i in range(amatrix.size):
        vph[i] = cmplx(0.0, 0.0)
        for j in range(amatrix.size):
            vph[i] = cadd(vph[i], cmul(amatrix.get_element(i+1, j+1), v012[j]))

def phase2ab0(vph, vab0, clarke_f):
    for i in range(clarke_f.size):
        vab0[i] = cmplx(0.0, 0.0)
        for j in range(clarke_f.size):
            vab0[i] = cadd(vab0[i], cmul(clarke_f.get_element(i+1, j+1), vph[j]))

def ab02phase(vph, vab0, clarke_r):
    for i in range(clarke_r.size):
        vph[i] = cmplx(0.0, 0.0)
        for j in range(clarke_r.size):
            vph[i] = cadd(vph[i], cmul(clarke_r.get_element(i+1, j+1), vab0[j]))

def terminal_power_in(v, i, nphases):
    result = cmplx(0.0, 0.0)
    for j in range(nphases):
        caccum(result, cmul(v[j], conjg(i[j])))
    return result

def pct_nema_unbalance(vph):
    vmag = [cabs(v) for v in vph]
    vavg = sum(vmag) / 3.0
    max_diff = max(abs(v - vavg) for v in vmag)
    if vavg != 0.0:
        return max_diff / vavg * 100.0
    else:
        return 0.0

def dbl_inc(x, y):
    x += y

def rcd_mean_and_std_dev(pdata, ndata):
    if ndata == 1:
        mean = pdata[0]
        std_dev = pdata[0]
        return mean, std_dev
    mean = rcd_sum(pdata, ndata) / ndata
    s = 0.0
    for i in range(ndata):
        s += (mean - pdata[i]) ** 2
    std_dev = math.sqrt(s / (ndata - 1))
    return mean, std_dev

def get_xr(a):
    if a.re != 0.0:
        result = a.im / a.re
        if abs(result) > 9999.0:
            result = 9999.0
    else:
        result = 9999.0
    return result

def parallel_z(z1, z2):
    denom = cadd(z1, z2)
    if denom.re != 0.0 or denom.im != 0.0:
        result = cdiv(cmul(z1, z2), denom)
    else:
        result = cmplx(0.0, 0.0)
    return result

def terminal_power_in(v, i, nphases):
    result = cmplx(0.0, 0.0)
    for j in range(nphases):
        caccum(result, cmul(v[j], conjg(i[j])))
    return result

def pct_nema_unbalance(vph):
    vmag = [cabs(v) for v in vph]
    vavg = sum(vmag) / 3.0
    max_diff = max(abs(v - vavg) for v in vmag)
    if vavg != 0.0:
        return max_diff / vavg * 100.0
    else:
        return 0.0

def dbl_inc(x, y):
    x += y

def set_clarke_matrices():
    clarke_f = TcMatrix(3)
    clarke_r = TcMatrix(3)
    set_clarke_matrices(clarke_f, clarke_r)
    return clarke_f, clarke_r

def etk_invert(a, norder):
    error = 0
    lt = [0] * norder
    t1 = 0.0
    k = 1
    for m in range(norder):
        for ll in range(norder):
            if lt[ll] != 1:
                rmy = abs(a[ll][ll]) - abs(t1)
                if rmy > 0.0:
                    t1 = a[ll][ll]
                    k = ll
        rmy = abs(t1)
        if rmy == 0.0:
            error = 2
            break
        t1 = 0.0
        lt[k] = 1
        for i in range(norder):
            if i != k:
                for j in range(norder):
                    if j != k:
                        a[i][j] -= a[i][k] * a[k][j] / a[k][k]
        a[k][k] = -1.0 / a[k][k]
        for i in range(norder):
            if i != k:
                a[i][k] *= a[k][k]
                a[k][i] *= a[k][k]
    for j in range(norder):
        for k in range(norder):
            a[j][k] = -a[j][k]
    return error

def calc_k_powers(kwkvar, v, i, n):
    for j in range(n):
        kwkvar[j] = cmulreal(cmul(v[j], conjg(i[j])), 0.001)

def phase2sym_comp(vph, v012, amatrix):
    for i in range(amatrix.size):
        v012[i] = cmplx(0.0, 0.0)
        for j in range(amatrix.size):
            v012[i] = cadd(v012[i], cmul(amatrix.get_element(i+1, j+1), vph[j]))

def sym_comp2phase(vph, v012, amatrix):
    for i in range(amatrix.size):
        vph[i] = cmplx(0.0, 0.0)
        for j in range(amatrix.size):
            vph[i] = cadd(vph[i], cmul(amatrix.get_element(i+1, j+1), v012[j]))

def phase2ab0(vph, vab0, clarke_f):
    for i in range(clarke_f.size):
        vab0[i] = cmplx(0.0, 0.0)
        for j in range(clarke_f.size):
            vab0[i] = cadd(vab0[i], cmul(clarke_f.get_element(i+1, j+1), vph[j]))

def ab02phase(vph, vab0, clarke_r):
    for i in range(clarke_r.size):
        vph[i] = cmplx(0.0, 0.0)
        for j in range(clarke_r.size):
            vph[i] = cadd(vph[i], cmul(clarke_r.get_element(i+1, j+1), vab0[j]))

def terminal_power_in(v, i, nphases):
    result = cmplx(0.0, 0.0)
    for j in range(nphases):
        caccum(result, cmul(v[j], conjg(i[j])))
    return result

def pct_nema_unbalance(vph):
    vmag = [cabs(v) for v in vph]
    vavg = sum(vmag) / 3.0
    max_diff = max(abs(v - vavg) for v in vmag)
    if vavg != 0.0:
        return max_diff / vavg * 100.0
    else:
        return 0.0

def dbl_inc(x, y):
    x += y

def set_clarke_matrices():
    clarke_f = TcMatrix(3)
    clarke_r = TcMatrix(3)
    set_clarke_matrices(clarke_f, clarke_r)
    return clarke_f, clarke_r

def etk_invert(a, norder):
    error = 0
    lt = [0] * norder
    t1 = 0.0
    k = 1
    for m in range(norder):
        for ll in range(norder):
            if lt[ll] != 1:
                rmy = abs(a[ll][ll]) - abs(t1)
                if rmy > 0.0:
                    t1 = a[ll][ll]
                    k = ll
        rmy = abs(t1)
        if rmy == 0.0:
            error = 2
            break
        t1 = 0.0
        lt[k] = 1
        for i in range(norder):
            if i != k:
                for j in range(norder):
                    if j != k:
                        a[i][j] -= a[i][k] * a[k][j] / a[k][k]
        a[k][k] = -1.0 / a[k][k]
        for i in range(norder):
            if i != k:
                a[i][k] *= a[k][k]
                a[k][i] *= a[k][k]
    for j in range(norder):
        for k in range(norder):
            a[j][k] = -a[j][k]
    return error

def calc_k_powers(kwkvar, v, i, n):
    for j in range(n):
        kwkvar[j] = cmulreal(cmul(v[j], conjg(i[j])), 0.001)

def phase2sym_comp(vph, v012, amatrix):
    for i in range(amatrix.size):
        v012[i] = cmplx(0.0, 0.0)
        for j in range(amatrix.size):
            v012[i] = cadd(v012[i], cmul(amatrix.get_element(i+1, j+1), vph[j]))

def sym_comp2phase(vph, v012, amatrix):
    for i in range(amatrix.size):
        vph[i] = cmplx(0.0, 0.0)
        for j in range(amatrix.size):
            vph[i] = cadd(vph[i], cmul(amatrix.get_element(i+1, j+1), v012[j]))

def phase2ab0(vph, vab0, clarke_f):
    for i in range(clarke_f.size):
        vab0[i] = cmplx(0.0, 0.0)
        for j in range(clarke_f.size):
            vab0[i] = cadd(vab0[i], cmul(clarke_f.get_element(i+1, j+1), vph[j]))

def ab02phase(vph, vab0, clarke_r):
    for i in range(clarke_r.size):
        vph[i] = cmplx(0.0, 0.0)
        for j in range(clarke_r.size):
            vph[i] = cadd(vph[i], cmul(clarke_r.get_element(i+1, j+1), vab0[j]))

def terminal_power_in(v, i, nphases):
    result = cmplx(0.0, 0.0)
    for j in range(nphases):
        caccum(result, cmul(v[j], conjg(i[j])))
    return result

def pct_nema_unbalance(vph):
    vmag = [cabs(v) for v in vph]
    vavg = sum(vmag) / 3.0
    max_diff = max(abs(v - vavg) for v in vmag)
    if vavg != 0.0:
        return max_diff / vavg * 100.0
    else:
        return 0.0

def dbl_inc(x, y):
    x += y

def set_clarke_matrices():
    clarke_f = TcMatrix(3)
    clarke_r = TcMatrix(3)
    set_clarke_matrices(clarke_f, clarke_r)
    return clarke_f, clarke_r

def etk_invert(a, norder):
    error = 0
    lt = [0] * norder
    t1 = 0.0
    k = 1
    for m in range(norder):
        for ll in range(norder):
            if lt[ll] != 1:
                rmy = abs(a[ll][ll]) - abs(t1)
                if rmy > 0.0:
                    t1 = a[ll][ll]
                    k = ll
        rmy = abs(t1)
        if rmy == 0.0:
            error = 2
            break
        t1 = 0.0
        lt[k] = 1
        for i in range(norder):
            if i != k:
                for j in range(norder):
                    if j != k:
                        a[i][j] -= a[i][k] * a[k][j] / a[k][k]
        a[k][k] = -1.0 / a[k][k]
        for i in range(norder):
            if i != k:
                a[i][k] *= a[k][k]
                a[k][i] *= a[k][k]
    for j in range(norder):
        for k in range(norder):
            a[j][k] = -a[j][k]
    return error

def calc_k_powers(kwkvar, v, i, n):
    for j in range(n):
        kwkvar[j] = cmulreal(cmul(v[j], conjg(i[j])), 0.001)

def phase2sym_comp(vph, v012, amatrix):
    for i in range(amatrix.size):
        v012[i] = cmplx(0.0, 0.0)
        for j in range(amatrix.size):
            v012[i] = cadd(v012[i], cmul(amatrix.get_element(i+1, j+1), vph[j]))

def sym_comp2phase(vph, v012, amatrix):
    for i in range(amatrix.size):
        vph[i] = cmplx(0.0, 0.0)
        for j in range(amatrix.size):
            vph[i] = cadd(vph[i], cmul(amatrix.get_element(i+1, j+1), v012[j]))

def phase2ab0(vph, vab0, clarke_f):
    for i in range(clarke_f.size):
        vab0[i] = cmplx(0.0, 0.0)
        for j in range(clarke_f.size):
            vab0[i] = cadd(vab0[i], cmul(clarke_f.get_element(i+1, j+1), vph[j]))

def ab02phase(vph, vab0, clarke_r):
    for i in range(clarke_r.size):
        vph[i] = cmplx(0.0, 0.0)
        for j in range(clarke_r.size):
            vph[i] = cadd(vph[i], cmul(clarke_r.get_element(i+1, j+1), vab0[j]))

def terminal_power_in(v, i, nphases):
    result = cmplx(0.0, 0.0)
    for j in range(nphases):
        caccum(result, cmul(v[j], conjg(i[j])))
    return result

def pct_nema_unbalance(vph):
    vmag = [cabs(v) for v in vph]
    vavg = sum(vmag) / 3.0
    max_diff = max(abs(v - vavg) for v in vmag)
    if vavg != 0.0:
        return max_diff / vavg * 100.0
    else:
        return 0.0

def dbl_inc(x, y):
    x += y

def set_clarke_matrices():
    clarke_f = TcMatrix(3)
    clarke_r = TcMatrix(3)
    set_clarke_matrices(clarke_f, clarke_r)
    return clarke_f, clarke_r

def etk_invert(a, norder):
    error = 0
    lt = [0] * norder
    t1 = 0.0
    k = 1
    for m in range(norder):
        for ll in range(norder):
            if lt[ll] != 1:
                rmy = abs(a[ll][ll]) - abs(t1)
                if rmy > 0.0:
                    t1 = a[ll][ll]
                    k = ll
        rmy = abs(t1)
        if rmy == 0.0:
            error = 2
            break
        t1 = 0.0
        lt[k] = 1
        for i in range(norder):
            if i != k:
                for j in range(norder):
                    if j != k:
                        a[i][j] -= a[i][k] * a[k][j] / a[k][k]
        a[k][k] = -1.0 / a[k][k]
        for i in range(norder):
            if i != k:
                a[i][k] *= a[k][k]
                a[k][i] *= a[k][k]
    for j in range(norder):
        for k in range(norder):
            a[j][k] = -a[j][k]
    return error

def calc_k_powers(kwkvar, v, i, n):
    for j in range(n):
        kwkvar[j] = cmulreal(cmul(v[j], conjg(i[j])), 0.001)

def phase2sym_comp(vph, v012, amatrix):
    for i in range(amatrix.size):
        v012[i] = cmplx(0.0, 0.0)
        for j in range(amatrix.size):
            v012[i] = cadd(v012[i], cmul(amatrix.get_element(i+1, j+1), vph[j]))

def sym_comp2phase(vph, v012, amatrix):
    for i in range(amatrix.size):
        vph[i] = cmplx(0.0, 0.0)
        for j in range(amatrix.size):
            vph[i] = cadd(vph[i], cmul(amatrix.get_element(i+1, j+1), v012[j]))

def phase2ab0(vph, vab0, clarke_f):
    for i in range(clarke_f.size):
        vab0[i] = cmplx(0.0, 0.0)
        for j in range(clarke_f.size):
            vab0[i] = cadd(vab0[i], cmul(clarke_f.get_element(i+1, j+1), vph[j]))

def ab02phase(vph, vab0, clarke_r):
    for i in range(clarke_r.size):
        vph[i] = cmplx(0.0, 0.0)
        for j in range(clarke_r.size):
            vph[i] = cadd(vph[i], cmul(clarke_r.get_element(i+1, j+1), vab0[j]))

def terminal_power_in(v, i, nphases):
    result = cmplx(0.0, 0.0)
    for j in range(nphases):
        caccum(result, cmul(v[j], conjg(i[j])))
    return result

def pct_nema_unbalance(vph):
    vmag = [cabs(v) for v in vph]
    vavg = sum(vmag) / 3.0
    max_diff = max(abs(v - vavg) for v in vmag)
    if vavg != 0.0:
        return max_diff / vavg * 100.0
    else:
        return 0.0

def dbl_inc(x, y):
    x += y

def set_clarke_matrices():
    clarke_f = TcMatrix(3)
    clarke_r = TcMatrix(3)
    set_clarke_matrices(clarke_f, clarke_r)
    return clarke_f, clarke_r

def etk_invert(a, norder):
    error = 0
    lt = [0] * norder
    t1 = 0.0
    k = 1
    for m in range(norder):
        for ll in range(norder):
            if lt[ll] != 1:
                rmy = abs(a[ll][ll]) - abs(t1)
                if rmy > 0.0:
                    t1 = a[ll][ll]
                    k = ll
        rmy = abs(t1)
        if rmy == 0.0:
            error = 2
            break
        t1 = 0.0
        lt[k] = 1
        for i in range(norder):
            if i != k:
                for j in range(norder):
                    if j != k:
                        a[i][j] -= a[i][k] * a[k][j] / a[k][k]
        a[k][k] = -1.0 / a[k][k]
        for i in range(norder):
            if i != k:
                a[i][k] *= a[k][k]
                a[k][i] *= a[k][k]
    for j in range(norder):
        for k in range(norder):
            a[j][k] = -a[j][k]
    return error

def calc_k_powers(kwkvar, v, i, n):
    for j in range(n):
        kwkvar[j] = cmulreal(cmul(v[j], conjg(i[j])), 0.001)

def phase2sym_comp(vph, v012, amatrix):
    for i in range(amatrix.size):
        v012[i] = cmplx(0.0, 0.0)
        for j in range(amatrix.size):
            v012[i] = cadd(v012[i], cmul(amatrix.get_element(i+1, j+1), vph[j]))

def sym_comp2phase(vph, v012, amatrix):
    for i in range(amatrix.size):
        vph[i] = cmplx(0.0, 0.0)
        for j in range(amatrix.size):
            vph[i] = cadd(vph[i], cmul(amatrix.get_element(i+1, j+1), v012[j]))

def phase2ab0(vph, vab0, clarke_f):
    for i in range(clarke_f.size):
        vab0[i] = cmplx(0.0, 0.0)
        for j in range(clarke_f.size):
            vab0[i] = cadd(vab0[i], cmul(clarke_f.get_element(i+1, j+1), vph[j]))

def ab02phase(vph, vab0, clarke_r):
    for i in range(clarke_r.size):
        vph[i] = cmplx(0.0, 0.0)
        for j in range(clarke_r.size):
            vph[i] = cadd(vph[i], cmul(clarke_r.get_element(i+1, j+1), vab0[j]))

def terminal_power_in(v, i, nphases):
    result = cmplx(0.0, 0.0)
    for j in range(nphases):
        caccum(result, cmul(v[j], conjg(i[j])))
    return result

def pct_nema_unbalance(vph):
    vmag = [cabs(v) for v in vph]
    vavg = sum(vmag) / 3.0
    max_diff = max(abs(v - vavg) for v in vmag)
    if vavg != 0.0:
        return max_diff / vavg * 100.0
    else:
        return 0.0

def dbl_inc(x, y):
    x += y

def set_clarke_matrices():
    clarke_f = TcMatrix(3)
    clarke_r = TcMatrix(3)
    set_clarke_matrices(clarke_f, clarke_r)
    return clarke_f, clarke_r

def etk_invert(a, norder):
    error = 0
    lt = [0] * norder
    t1 = 0.0
    k = 1
    for m in range(norder):
        for ll in range(norder):
            if lt[ll] != 1:
                rmy = abs(a[ll][ll]) - abs(t1)
                if rmy > 0.0:
                    t1 = a[ll][ll]
                    k = ll
        rmy = abs(t1)
        if rmy == 0.0:
            error = 2
            break
        t1 = 0.0
        lt[k] = 1
        for i in range(norder):
            if i != k:
                for j in range(norder):
                    if j != k:
                        a[i][j] -= a[i][k] * a[k][j] / a[k][k]
        a[k][k] = -1.0 / a[k][k]
        for i in range(norder):
            if i != k:
                a[i][k] *= a[k][k]
                a[k][i] *= a[k][k]
    for j in range(norder):
        for k in range(norder):
            a[j][k] = -a[j][k]
    return error

def calc_k_powers(kwkvar, v, i, n):
    for j in range(n):
        kwkvar[j] = cmulreal(cmul(v[j], conjg(i[j])), 0.001)

def phase2sym_comp(vph, v012, amatrix):
    for i in range(amatrix.size):
        v012[i] = cmplx(0.0, 0.0)
        for j in range(amatrix.size):
            v012[i] = cadd(v012[i], cmul(amatrix.get_element(i+1, j+1), vph[j]))

def sym_comp2phase(vph, v012, amatrix):
    for i in range(amatrix.size):
        vph[i] = cmplx(0.0, 0.0)
        for j in range(amatrix.size):
            vph[i] = cadd(vph[i], cmul(amatrix.get_element(i+1, j+1), v012[j]))

def phase2ab0(vph, vab0, clarke_f):
    for i in range(clarke_f.size):
        vab0[i] = cmplx(0.0, 0.0)
        for j in range(clarke_f.size):
            vab0[i] = cadd(vab0[i], cmul(clarke_f.get_element(i+1, j+1), vph[j]))

def ab02phase(vph, vab0, clarke_r):
    for i in range(clarke_r.size):
        vph[i] = cmplx(0.0, 0.0)
        for j in range(clarke_r.size):
            vph[i] = cadd(vph[i], cmul(clarke_r.get_element(i+1, j+1), vab0[j]))

def terminal_power_in(v, i, nphases):
    result = cmplx(0.0, 0.0)
    for j in range(nphases):
        caccum(result, cmul(v[j], conjg(i[j])))
    return result

def pct_nema_unbalance(vph):
    vmag = [cabs(v) for v in vph]
    vavg = sum(vmag) / 3.0
    max_diff = max(abs(v - vavg) for v in vmag)
    if vavg != 0.0:
        return max_diff / vavg * 100.0
    else:
        return 0.0

def dbl_inc(x, y):
    x += y

def set_clarke_matrices():
    clarke_f = TcMatrix(3)
    clarke_r = TcMatrix(3)
    set_clarke_matrices(clarke_f, clarke_r)
    return clarke_f, clarke_r

def etk_invert(a, norder):
    error = 0
    lt = [0] * norder
    t1 = 0.0
    k = 1
    for m in range(norder):
        for ll in range(norder):
            if lt[ll] != 1:
                rmy = abs(a[ll][ll]) - abs(t1)
                if rmy > 0.0:
                    t1 = a[ll][ll]
                    k = ll
        rmy = abs(t1)
        if rmy == 0.0:
            error = 2
            break
        t1 = 0.0
        lt[k] = 1
        for i in range(norder):
            if i != k:
                for j in range(norder):
                    if j != k:
                        a[i][j] -= a[i][k] * a[k][j] / a[k][k]
        a[k][k] = -1.0 / a[k][k]
        for i in range(norder):
            if i != k:
                a[i][k] *= a[k][k]
                a[k][i] *= a[k][k]
    for j in range(norder):
        for k in range(norder):
            a[j][k] = -a[j][k]
    return error

def calc_k_powers(kwkvar, v, i, n):
    for j in range(n):
        kwkvar[j] = cmulreal(cmul(v[j], conjg(i[j])), 0.001)

def phase2sym_comp(vph, v012, amatrix):
    for i in range(amatrix.size):
        v012[i] = cmplx(0.0, 0.0)
        for j in range(amatrix.size):
            v012[i] = cadd(v012[i], cmul(amatrix.get_element(i+1, j+1), vph[j]))

def sym_comp2phase(vph, v012, amatrix):
    for i in range(amatrix.size):
        vph[i] = cmplx(0.0, 0.0)
        for j in range(amatrix.size):
            vph[i] = cadd(vph[i], cmul(amatrix.get_element(i+1, j+1), v012[j]))

def phase2ab0(vph, vab0, clarke_f):
    for i in range(clarke_f.size):
        vab0[i] = cmplx(0.0, 0.0)
        for j in range(clarke_f.size):
            vab0[i] = cadd(vab0[i], cmul(clarke_f.get_element(i+1, j+1), vph[j]))

def ab02phase(vph, vab0, clarke_r):
    for i in range(clarke_r.size):
        vph[i] = cmplx(0.0, 0.0)
        for j in range(clarke_r.size):
            vph[i] = cadd(vph[i], cmul(clarke_r.get_element(i+1, j+1), vab0[j]))

def terminal_power_in(v, i, nphases):
    result = cmplx(0.0, 0.0)
    for j in range(nphases):
        caccum(result, cmul(v[j], conjg(i[j])))
    return result

def pct_nema_unbalance(vph):
    vmag = [cabs(v) for v in vph]
    vavg = sum(vmag) / 3.0
    max_diff = max(abs(v - vavg) for v in vmag)
    if vavg != 0.0:
        return max_diff / vavg * 100.0
    else:
        return 0.0

def dbl_inc(x, y):
    x += y

def set_clarke_matrices():
    clarke_f = TcMatrix(3)
    clarke_r = TcMatrix(3)
    set_clarke_matrices(clarke_f, clarke_r)
    return clarke_f, clarke_r

def etk_invert(a, norder):
    error = 0
    lt = [0] * norder
    t1 = 0.0
    k = 1
    for m in range(norder):
        for ll in range(norder):
            if lt[ll] != 1:
                rmy = abs(a[ll][ll]) - abs(t1)
                if rmy > 0.0:
                    t1 = a[ll][ll]
                    k = ll
        rmy = abs(t1)
        if rmy == 0.0:
            error = 2
            break
        t1 = 0.0
        lt[k] = 1
        for i in range(norder):
            if i != k:
                for j in range(norder):
                    if j != k:
                        a[i][j] -= a[i][k] * a[k][j] / a[k][k]
        a[k][k] = -1.0 / a[k][k]
        for i in range(norder):
            if i != k:
                a[i][k] *= a[k][k]
                a[k][i] *= a[k][k]
    for j in range(norder):
        for k in range(norder):
            a[j][k] = -a[j][k]
    return error

def calc_k_powers(kwkvar, v, i, n):
    for j in range(n):
        kwkvar[j] = cmulreal(cmul(v[j], conjg(i[j])), 0.001)

def phase2sym_comp(vph, v012, amatrix):
    for i in range(amatrix.size):
        v012[i] = cmplx(0.0, 0.0)
        for j in range(amatrix.size):
            v012[i] = cadd(v012[i], cmul(amatrix.get_element(i+1, j+1), vph[j]))

def sym_comp2phase(vph, v012, amatrix):
    for i in range(amatrix.size):
        vph[i] = cmplx(0.0, 0.0)
        for j in range(amatrix.size):
            vph[i] = cadd(vph[i], cmul(amatrix.get_element(i+1, j+1), v012[j]))

def phase2ab0(vph, vab0, clarke_f):
    for i in range(clarke_f.size):
        vab0[i] = cmplx(0.0, 0.0)
        for j in range(clarke_f.size):
            vab0[i] = cadd(vab0[i], cmul(clarke_f.get_element(i+1, j+1), vph[j]))

def ab02phase(vph, vab0, clarke_r):
    for i in range(clarke_r.size):
        vph[i] = cmplx(0.0, 0.0)
        for j in range(clarke_r.size):
            vph[i] = cadd(vph[i], cmul(clarke_r.get_element(i+1, j+1), vab0[j]))

def terminal_power_in(v, i, nphases):
    result = cmplx(0.0, 0.0)
    for j in range(nphases):
        caccum(result, cmul(v[j], conjg(i[j])))
    return result

def pct_nema_unbalance(vph):
    vmag = [cabs(v) for v in vph]
    vavg = sum(vmag) / 3.0
    max_diff = max(abs(v - vavg) for v in vmag)
    if vavg != 0.0:
        return max_diff / vavg * 100.0
    else:
        return 0.0

def dbl_inc(x, y):
    x += y

def set_clarke_matrices():
    clarke_f = TcMatrix(3)
    clarke_r = TcMatrix(3)
    set_clarke_matrices(clarke_f, clarke_r)
    return clarke_f, clarke_r

def etk_invert(a, norder):
    error = 0
    lt = [0] * norder
    t1 = 0.0
    k = 1
    for m in range(norder):
        for ll in range(norder):
            if lt[ll] != 1:
                rmy = abs(a[ll][ll]) - abs(t1)
                if rmy > 0.0:
                    t1 = a[ll][ll]
                    k = ll
        rmy = abs(t1)
        if rmy == 0.0:
            error = 2
            break
        t1 = 0.0
        lt[k] = 1
        for i in range(norder):
            if i != k:
                for j in range(norder):
                    if j != k:
                        a[i][j] -= a[i][k] * a[k][j] / a[k][k]
        a[k][k] = -1.0 / a[k][k]
        for i in range(norder):
            if i != k:
                a[i][k] *= a[k][k]
                a[k][i] *= a[k][k]
    for j in range(norder):
        for k in range(norder):
            a[j][k] = -a[j][k]
    return error

def calc_k_powers(kwkvar, v, i, n):
    for j in range(n):
        kwkvar[j] = cmulreal(cmul(v[j], conjg(i[j])), 0.001)

def phase2sym_comp(vph, v012, amatrix):
    for i in range(amatrix.size):
        v012[i] = cmplx(0.0, 0.0)
        for j in range(amatrix.size):
            v012[i] = cadd(v012[i], cmul(amatrix.get_element(i+1, j+1), vph[j]))

def sym_comp2phase(vph, v012, amatrix):
    for i in range(amatrix.size):
        vph[i] = cmplx(0.0, 0.0)
        for j in range(amatrix.size):
            vph[i] = cadd(vph[i], cmul(amatrix.get_element(i+1, j+1), v012[j]))

def phase2ab0(vph, vab0, clarke_f):
    for i in range(clarke_f.size):
        vab0[i] = cmplx(0.0, 0.0)
        for j in

